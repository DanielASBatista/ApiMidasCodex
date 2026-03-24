using Microsoft.EntityFrameworkCore;
using ProjetoMidasAPI.Data;
using ProjetoMidasAPI.DTOs.Transactions;
using ProjetoMidasAPI.Models;
using ProjetoMidasAPI.Models.Enuns;

namespace ProjetoMidasAPI.Services
{
    public class TransactionService
    {
        private readonly AppDbContext _context;

        public TransactionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Transaction>> GetTransactionsAsync(
            int userId,
            TransactionType? type = null,
            TransactionStatus? status = null,
            TransactionOrigin? origin = null)
        {
            await RefreshLateTransactionsAsync(userId);

            var query = _context.Transactions
                .Where(t => t.UserId == userId);

            if (type.HasValue)
            {
                query = query.Where(t => t.Type == type.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            if (origin.HasValue)
            {
                query = query.Where(t => t.Origin == origin.Value);
            }

            return await query
                .OrderBy(t => t.Date)
                .ThenBy(t => t.Id)
                .ToListAsync();
        }

        public async Task<Transaction?> GetTransactionByIdAsync(int userId, int id)
        {
            await RefreshLateTransactionsAsync(userId);

            return await _context.Transactions
                .FirstOrDefaultAsync(t => t.UserId == userId && t.Id == id);
        }

        public async Task<IReadOnlyList<Transaction>> CreateTransactionAsync(int userId, CreateTransactionDto dto)
        {
            if (dto.Recurrence == null)
            {
                var transaction = BuildManualTransaction(userId, dto);
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();
                return new[] { transaction };
            }

            ValidateRecurrence(dto.Recurrence);

            var recurrenceGroupId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;
            var transactions = new List<Transaction>();

            for (var occurrence = 1; occurrence <= dto.Recurrence.Occurrences; occurrence++)
            {
                transactions.Add(new Transaction
                {
                    UserId = userId,
                    Type = TransactionType.PROJECTED,
                    Status = ResolveProjectedStatus(CalculateRecurrenceDate(dto.Date, dto.Recurrence, occurrence)),
                    Origin = TransactionOrigin.RECURRENCE,
                    Amount = dto.Amount,
                    Date = CalculateRecurrenceDate(dto.Date, dto.Recurrence, occurrence),
                    Description = dto.Description,
                    Category = dto.Category,
                    RecurrenceGroupId = recurrenceGroupId,
                    OccurrenceNumber = occurrence,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt
                });
            }

            _context.Transactions.AddRange(transactions);
            await _context.SaveChangesAsync();

            return transactions;
        }

        public async Task<Transaction?> ConfirmTransactionAsync(int userId, int transactionId)
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.UserId == userId && t.Id == transactionId);

            if (transaction == null)
            {
                return null;
            }

            if (transaction.Type == TransactionType.PROJECTED)
            {
                transaction.Type = TransactionType.REALIZED;
            }

            transaction.Status = transaction.Date.Date < DateTime.UtcNow.Date
                ? TransactionStatus.LATE
                : TransactionStatus.CONFIRMED;
            transaction.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<IReadOnlyList<Transaction>> CreateLoanAsync(int userId, CreateLoanDto dto)
        {
            var loanId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;
            var installmentAmount = CalculateInstallmentAmount(dto.TotalAmount, dto.InterestRate, dto.NumberOfInstallments);
            var transactions = new List<Transaction>();

            for (var installment = 1; installment <= dto.NumberOfInstallments; installment++)
            {
                var dueDate = dto.FirstInstallmentDate.Date.AddMonths(installment - 1);

                transactions.Add(new Transaction
                {
                    UserId = userId,
                    Type = TransactionType.PROJECTED,
                    Status = ResolveProjectedStatus(dueDate),
                    Origin = TransactionOrigin.LOAN,
                    Amount = installmentAmount,
                    Date = dueDate,
                    Description = $"{dto.Description} - parcela {installment}/{dto.NumberOfInstallments}",
                    Category = dto.Category,
                    LoanId = loanId,
                    OccurrenceNumber = installment,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt
                });
            }

            _context.Transactions.AddRange(transactions);
            await _context.SaveChangesAsync();

            return transactions;
        }

        public async Task<(IReadOnlyList<Transaction> Transactions, bool InboundCreated)> ConfirmLoanAsync(
            int userId,
            Guid loanId,
            ConfirmLoanDto? dto)
        {
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId && t.LoanId == loanId)
                .OrderBy(t => t.OccurrenceNumber)
                .ToListAsync();

            if (transactions.Count == 0)
            {
                return (Array.Empty<Transaction>(), false);
            }

            var now = DateTime.UtcNow;

            foreach (var transaction in transactions)
            {
                transaction.Type = TransactionType.REALIZED;
                transaction.Status = transaction.Date.Date < now.Date
                    ? TransactionStatus.LATE
                    : TransactionStatus.CONFIRMED;
                transaction.UpdatedAt = now;
            }

            var inboundCreated = false;
            if (dto?.CreateInboundTransaction != false)
            {
                var totalReceived = transactions.Sum(t => t.Amount);
                var inbound = new Transaction
                {
                    UserId = userId,
                    Type = TransactionType.REALIZED,
                    Status = TransactionStatus.CONFIRMED,
                    Origin = TransactionOrigin.LOAN,
                    Amount = totalReceived,
                    Date = dto?.ReceivedAt?.Date ?? now.Date,
                    Description = dto?.InboundDescription ?? "Entrada do emprestimo",
                    Category = dto?.InboundCategory ?? "Loan Credit",
                    LoanId = loanId,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.Transactions.Add(inbound);
                transactions.Add(inbound);
                inboundCreated = true;
            }

            await _context.SaveChangesAsync();
            return (transactions, inboundCreated);
        }

        private Transaction BuildManualTransaction(int userId, CreateTransactionDto dto)
        {
            var now = DateTime.UtcNow;
            var type = dto.Type;

            return new Transaction
            {
                UserId = userId,
                Type = type,
                Status = type == TransactionType.REALIZED
                    ? TransactionStatus.CONFIRMED
                    : ResolveProjectedStatus(dto.Date),
                Origin = TransactionOrigin.MANUAL,
                Amount = dto.Amount,
                Date = dto.Date,
                Description = dto.Description,
                Category = dto.Category,
                CreatedAt = now,
                UpdatedAt = now
            };
        }

        private async Task RefreshLateTransactionsAsync(int userId)
        {
            var now = DateTime.UtcNow.Date;
            var lateTransactions = await _context.Transactions
                .Where(t => t.UserId == userId
                    && t.Type == TransactionType.PROJECTED
                    && t.Status == TransactionStatus.PENDING
                    && t.Date.Date < now)
                .ToListAsync();

            if (lateTransactions.Count == 0)
            {
                return;
            }

            foreach (var transaction in lateTransactions)
            {
                transaction.Status = TransactionStatus.LATE;
                transaction.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        private static TransactionStatus ResolveProjectedStatus(DateTime date)
        {
            return date.Date < DateTime.UtcNow.Date
                ? TransactionStatus.LATE
                : TransactionStatus.PENDING;
        }

        private static void ValidateRecurrence(RecurrenceDto recurrence)
        {
            if (recurrence.Frequency == RecurrenceFrequency.MONTHLY)
            {
                if (!recurrence.MonthlyMode.HasValue)
                {
                    throw new InvalidOperationException("MonthlyMode is required for monthly recurrences.");
                }

                if (recurrence.MonthlyMode == MonthlyRecurrenceMode.FIXED_DAY && !recurrence.DayOfMonth.HasValue)
                {
                    throw new InvalidOperationException("DayOfMonth is required for fixed-day monthly recurrences.");
                }

                if (recurrence.MonthlyMode == MonthlyRecurrenceMode.INTERVAL_DAYS && !recurrence.IntervalDays.HasValue)
                {
                    throw new InvalidOperationException("IntervalDays is required for interval monthly recurrences.");
                }
            }
        }

        private static DateTime CalculateRecurrenceDate(DateTime startDate, RecurrenceDto recurrence, int occurrence)
        {
            if (occurrence == 1)
            {
                return startDate.Date;
            }

            return recurrence.Frequency switch
            {
                RecurrenceFrequency.WEEKLY => startDate.Date.AddDays(7 * (occurrence - 1)),
                RecurrenceFrequency.MONTHLY when recurrence.MonthlyMode == MonthlyRecurrenceMode.INTERVAL_DAYS
                    => startDate.Date.AddDays((recurrence.IntervalDays ?? 30) * (occurrence - 1)),
                RecurrenceFrequency.MONTHLY => CalculateFixedDayMonthlyDate(startDate.Date, recurrence.DayOfMonth ?? startDate.Day, occurrence - 1),
                _ => throw new InvalidOperationException("Unsupported recurrence configuration.")
            };
        }

        private static DateTime CalculateFixedDayMonthlyDate(DateTime startDate, int dayOfMonth, int monthOffset)
        {
            var candidate = new DateTime(startDate.Year, startDate.Month, 1).AddMonths(monthOffset);
            var validDay = Math.Min(dayOfMonth, DateTime.DaysInMonth(candidate.Year, candidate.Month));
            return new DateTime(candidate.Year, candidate.Month, validDay);
        }

        private static decimal CalculateInstallmentAmount(decimal totalAmount, decimal interestRate, int numberOfInstallments)
        {
            var factor = 1 + (interestRate / 100m);
            var totalWithInterest = totalAmount * factor;
            return decimal.Round(totalWithInterest / numberOfInstallments, 2, MidpointRounding.AwayFromZero);
        }
    }
}

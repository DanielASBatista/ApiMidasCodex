using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjetoMidasAPI.DTOs.Transactions;
using ProjetoMidasAPI.Models.Enuns;
using ProjetoMidasAPI.Services;

namespace ProjetoMidasAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly TransactionService _transactionService;

        public TransactionsController(TransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        private int UserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] TransactionType? type,
            [FromQuery] TransactionStatus? status,
            [FromQuery] TransactionOrigin? origin)
        {
            var transactions = await _transactionService.GetTransactionsAsync(UserId, type, status, origin);
            return Ok(transactions);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var transaction = await _transactionService.GetTransactionByIdAsync(UserId, id);
            return transaction == null ? NotFound() : Ok(transaction);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateTransactionDto dto)
        {
            var created = await _transactionService.CreateTransactionAsync(UserId, dto);
            return Ok(created);
        }

        [HttpPost("{id:int}/confirm")]
        public async Task<IActionResult> ConfirmTransaction(int id)
        {
            var transaction = await _transactionService.ConfirmTransactionAsync(UserId, id);
            return transaction == null ? NotFound() : Ok(transaction);
        }

        [HttpPost("loans")]
        public async Task<IActionResult> CreateLoan(CreateLoanDto dto)
        {
            var created = await _transactionService.CreateLoanAsync(UserId, dto);
            return Ok(created);
        }

        [HttpPost("loans/{loanId:guid}/confirm")]
        public async Task<IActionResult> ConfirmLoan(Guid loanId, ConfirmLoanDto? dto)
        {
            var result = await _transactionService.ConfirmLoanAsync(UserId, loanId, dto);

            if (result.Transactions.Count == 0)
            {
                return NotFound();
            }

            return Ok(new
            {
                message = "Emprestimo confirmado! Confira informacoes na tela de lancamentos.",
                inboundTransactionCreated = result.InboundCreated,
                transactions = result.Transactions
            });
        }
    }
}

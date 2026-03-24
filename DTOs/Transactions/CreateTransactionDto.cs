using System.ComponentModel.DataAnnotations;
using ProjetoMidasAPI.Models.Enuns;

namespace ProjetoMidasAPI.DTOs.Transactions
{
    public class CreateTransactionDto
    {
        public TransactionType Type { get; set; } = TransactionType.REALIZED;

        [Range(0.01, 999999999999d)]
        public decimal Amount { get; set; }

        public DateTime Date { get; set; }

        [Required]
        [MaxLength(120)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(60)]
        public string Category { get; set; } = string.Empty;

        public RecurrenceDto? Recurrence { get; set; }
    }
}

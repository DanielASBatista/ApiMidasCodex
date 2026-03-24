using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ProjetoMidasAPI.Models.Enuns;

namespace ProjetoMidasAPI.Models
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [JsonIgnore]
        public Usuario? Usuario { get; set; }

        public TransactionType Type { get; set; }

        public TransactionStatus Status { get; set; }

        public TransactionOrigin Origin { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime Date { get; set; }

        [Required]
        [MaxLength(120)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(60)]
        public string Category { get; set; } = string.Empty;

        public Guid? RecurrenceGroupId { get; set; }

        public Guid? LoanId { get; set; }

        public int? SourceTransactionId { get; set; }

        public int? OccurrenceNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

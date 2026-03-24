using System.ComponentModel.DataAnnotations;

namespace ProjetoMidasAPI.DTOs.Transactions
{
    public class CreateLoanDto
    {
        [Range(0.01, 999999999999d)]
        public decimal TotalAmount { get; set; }

        [Range(0d, 1000d)]
        public decimal InterestRate { get; set; }

        [Range(1, 600)]
        public int NumberOfInstallments { get; set; }

        public DateTime FirstInstallmentDate { get; set; }

        [Required]
        [MaxLength(120)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(60)]
        public string Category { get; set; } = "Loan";
    }
}

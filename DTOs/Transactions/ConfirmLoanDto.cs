namespace ProjetoMidasAPI.DTOs.Transactions
{
    public class ConfirmLoanDto
    {
        public bool CreateInboundTransaction { get; set; } = true;

        public DateTime? ReceivedAt { get; set; }

        public string? InboundDescription { get; set; }

        public string? InboundCategory { get; set; }
    }
}

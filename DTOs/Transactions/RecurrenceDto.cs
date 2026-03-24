using System.ComponentModel.DataAnnotations;
using ProjetoMidasAPI.Models.Enuns;

namespace ProjetoMidasAPI.DTOs.Transactions
{
    public class RecurrenceDto
    {
        public RecurrenceFrequency Frequency { get; set; }

        [Range(1, 120)]
        public int Occurrences { get; set; }

        public MonthlyRecurrenceMode? MonthlyMode { get; set; }

        [Range(1, 31)]
        public int? DayOfMonth { get; set; }

        [Range(1, 365)]
        public int? IntervalDays { get; set; }
    }
}

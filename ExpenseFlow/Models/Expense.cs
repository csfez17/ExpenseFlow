using System.ComponentModel.DataAnnotations;

namespace ExpenseFlow.Models
{
    public class Expense
    {
        public enum ExpenseCategory
        {
            Car,
            Food,
            Operating,
            IT,
            Training,
            Other
        }
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClientId { get; set; }
        public string? CompanyName { get; set; }


        [Required]
        public DateTime TransactionTime { get; set; }


        [Required]
        public decimal AmountBeforeVAT { get; set; }

        [Required]
        public decimal AmountAfterVAT { get; set; }
        public string? InvoiceNumber { get; set; }
        public ExpenseCategory Category { get; set; }



    }
}

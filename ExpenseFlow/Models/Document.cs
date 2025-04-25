using System.ComponentModel.DataAnnotations;

namespace ExpenseFlow.Models
{
    public class Document
    {
        public enum DocumentType
        {
            Receipt,
            Invoice,
        }
        [Key]
        public int Id { get; private set; } // Primary key
        public Expense? Expense { get; set; }
        public int ClientId { get; set; }
        public DocumentType DocType { get; set; }

        [StringLength(50, ErrorMessage = "Reseller number cannot exceed 50 characters")]
        public string ResellerNumber { get; set; }


        public string ServiceProvided { get; set; }


    }
}

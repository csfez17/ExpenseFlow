using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;

namespace ExpenseFlow.Models
{
    public class Client
    {
        [Key]
        public int Id { get; set; }

        //[Required(ErrorMessage = "First name is required")]
        public string FirstName { get; set; }

        //[Required(ErrorMessage = "Last name is required")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
        public string Password { get; set; }

        public List<Document> Documents { get; set; } = new List<Document>();
        public List<Expense> Expenses { get; set; } = new List<Expense>();



    }
}

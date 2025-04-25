using ExpenseFlow.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace ExpenseFlow.Data
{
    public class ClientsDbContext : DbContext
    {
        public ClientsDbContext(DbContextOptions<ClientsDbContext> options) : base(options)
        {

        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<Expense> Expenses { get; set; }
    }
}

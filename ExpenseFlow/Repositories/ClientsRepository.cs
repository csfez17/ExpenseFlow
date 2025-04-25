using ExpenseFlow.Data;
using ExpenseFlow.Models;
using Microsoft.EntityFrameworkCore;


namespace ExpenseFlow.Repositories
{
    public class ClientsRepository
    {
        private readonly ClientsDbContext _context;

        public ClientsRepository(ClientsDbContext context)
        {
            _context = context;
        }

        public async Task<List<Client>> GetAllClientsAsync()
        {
            return await _context.Clients.ToListAsync(); //This LINQ query tells EF Core to fetch all records from the Clients table (SELECT * FROM Clients)

            //    return await _context.Clients
            //.FromSqlRaw("SELECT * FROM Clients")
            //.ToListAsync();
        }
        public async Task<List<Expense>> GetAllExpensesAsync()
        {
            return await _context.Expenses.ToListAsync();

        }


        public Client RegisterNewClient(Client client)
        {
            _context.Clients.Add(client);
            _context.SaveChanges();

            return client;

        }

        public void AddNewBudgetAndDocument(Expense budget, Document document)
        {
            _context.Expenses.Add(budget);
            _context.Documents.Add(document);
            _context.SaveChanges();

        }

        public async Task<Expense> GetByIdAsync(int id)
        {
            return await _context.Expenses.FindAsync(id);
        }

        public async Task UpdateAsync(Expense expense)
        {
            _context.Expenses.Update(expense);
            await _context.SaveChangesAsync();
        }

    }
}

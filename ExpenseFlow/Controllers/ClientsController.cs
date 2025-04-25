using ExpenseFlow.Models;
using ExpenseFlow.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using static ExpenseFlow.Models.Expense;




namespace ExpenseFlow.Controllers
{
    public class ClientsController : Controller
    {
        private readonly ClientsService _service;

        public ClientsController(ClientsService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> AllClients()
        {
            var clients = await _service.GetAllClientsAsync();

            return View(clients);

        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(Client client)
        {

            if (!ModelState.IsValid)
            {
                return View("Register", client);
            }

            try
            {
                Client newClient = _service.RegisterNewClient(client);
                HttpContext.Session.SetString("Client", JsonConvert.SerializeObject(newClient));
                return RedirectToAction("Welcome");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while registering the client. Please try again.");
                return View("Register", client);
            }

        }

        [HttpGet]
        public IActionResult Login()
        {
            var client = GetLoggedInClient();
            if (client == null)
            {
                return View(new LoginViewModel());
            }
            return RedirectToAction("Welcome", client);

        }


        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel loginModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View("Login", loginModel);
                }
                var storedClient = (await _service.GetAllClientsAsync()).FirstOrDefault(c => c.Email == loginModel.Email);
                if (storedClient == null)
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                }
                else
                {

                    bool correctPassword = BCrypt.Net.BCrypt.Verify(loginModel.Password, storedClient.Password);

                    if (!correctPassword)
                    {
                        // Password doesn't match
                        ModelState.AddModelError("", "Invalid email or password.");
                    }
                    else
                    {
                        var clientSessionData = new { storedClient.Id, storedClient.Email, storedClient.FirstName, storedClient.LastName };
                        HttpContext.Session.SetString("Client", JsonConvert.SerializeObject(clientSessionData));
                        return RedirectToAction("Welcome");
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while registering the client. Please try again.");
            }
            return View();

        }

        public IActionResult Logout()
        {
            return View("Login");
        }
        public IActionResult Welcome()
        {
            // Retrieve the client from the session
            var clientJson = HttpContext.Session.GetString("Client");
            if (string.IsNullOrEmpty(clientJson))
            {
                // If no client is found in the session, redirect to registration
                return RedirectToAction("Register");
            }

            var client = JsonConvert.DeserializeObject<Client>(clientJson);
            return View(client);
        }

        [HttpGet]
        public async Task<IActionResult> AllExpenses()
        {
            var client = GetLoggedInClient();
            if (client == null)
            {
                return View();
            }
            var expenses = await _service.GetAllExpensesFromUserAsync(client.Id);
            if (expenses.Count == 0)
            {
                ViewBag.Message = "No expenses found.";
                return View("ExpenseManagement", new List<Expense>()); // Return empty list to avoid null reference in view
            }
            return View("ExpenseManagement", expenses);

        }

        [HttpPost]
        public async Task<IActionResult> UpdateExpenseCategory(int id, ExpenseCategory category)
        {
            var expense = await _service.GetExpenseByIdAsync(id);
            if (expense == null)
            {
                return NotFound();
            }

            expense.Category = category;
            await _service.UpdateExpenseAsync(expense);

            return RedirectToAction("AllExpenses");
        }


        [HttpGet]
        public IActionResult DocsUploading()
        {
            return View();
        }

        [HttpPost]
        [RequestSizeLimit(100 * 1024 * 1024)]
        public async Task<IActionResult> DocsUploading(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ViewBag.Message = "Please select a file to upload.";
                return View();
            }

            if (!_service.IsSupportedFileType(file.ContentType))
            {
                ViewBag.Message = "Unsupported file type.";
                return View();
            }

            string extractedText = "";

            try
            {
                extractedText = await _service.ExtractTextAsync(file);

                var client = GetLoggedInClient();
                if (client == null)
                {
                    ViewBag.Message("Client session has expired.");
                    return View();
                }

                var analysis = _service.AnalyzeText(extractedText);

                var analysisResult = _service.MapToEntities(analysis, client.Id);

                ViewBag.Message = "File processed and data saved!";
                ViewBag.ExtractedText = extractedText;
                ViewBag.AnalysisResult = analysis;
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"Error processing file: {ex.Message}";
            }

            return View();
        }

        private Client? GetLoggedInClient()
        {
            var clientJson = HttpContext.Session.GetString("Client");
            return string.IsNullOrEmpty(clientJson) ? null : JsonConvert.DeserializeObject<Client>(clientJson);
        }




        // Filtering

        [HttpGet]
        public async Task<IActionResult> FilterExpenses(string? companyName, decimal? amountMin, decimal? amountMax, DateTime? startDate, DateTime? endDate, int? category)
        {
            var client = GetLoggedInClient();
            if (client == null)
            {
                return View();
            }
            var allExpenses = await _service.FilterExpenses(companyName, amountMin, amountMax, startDate, endDate, category, client.Id);

            return View("ExpenseManagement", allExpenses);
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

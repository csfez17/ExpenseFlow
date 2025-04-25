using ExpenseFlow.Models;
using ExpenseFlow.Repositories;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Tesseract;

namespace ExpenseFlow.Services
{
    public class ClientsService
    {
        private readonly ClientsRepository _repository;

        public ClientsService(ClientsRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Client>> GetAllClientsAsync()
        {
            return await _repository.GetAllClientsAsync();
        }

        public async Task<List<Expense>> GetAllExpensesFromUserAsync(int ClientId)
        {

            var AllExpenses = await _repository.GetAllExpensesAsync();
            return AllExpenses.Where(e => e.ClientId == ClientId).ToList();
        }

        public Client RegisterNewClient(Client client)
        {
            client.Password = BCrypt.Net.BCrypt.HashPassword(client.Password);
            return _repository.RegisterNewClient(client);
        }

        public string ExtractTextFromImage(Stream imageStream)
        {
            var ocrResult = "";

            var tessDataPath = Path.Combine(Directory.GetCurrentDirectory(), "tessdata");

            using var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);
            using var img = Pix.LoadFromMemory(ReadAllBytes(imageStream));
            using var page = engine.Process(img);

            ocrResult = page.GetText();

            return ocrResult;
        }

        private byte[] ReadAllBytes(Stream input)
        {
            using var ms = new MemoryStream();
            input.CopyTo(ms);
            return ms.ToArray();
        }

        public DocumentAnalysisResult MapToEntities(DocumentAnalysisResult analysis, int clientId)
        {
            DateTime time = DateTime.TryParse(analysis.TransactionDate, out var date1) ? date1 : DateTime.Now;
            var expense = new Expense
            {
                ClientId = clientId,
                CompanyName = analysis.CompanyName,
                TransactionTime = DateTime.TryParse(analysis.TransactionDate, out var date) ? date : DateTime.Now,
                AmountBeforeVAT = decimal.TryParse(analysis.AmountBeforeVAT, out var beforeVat) ? beforeVat : 0,
                AmountAfterVAT = decimal.TryParse(analysis.AmountAfterVAT, out var afterVat) ? afterVat : 0,
                InvoiceNumber = analysis.InvoiceNumber,
                Category = Expense.ExpenseCategory.Other
            };

            var document = new Document
            {
                ClientId = clientId,
                DocType = analysis.DocumentType == "Invoice" ? Document.DocumentType.Invoice : Document.DocumentType.Receipt,
                ResellerNumber = analysis.ResellerNumber,
                ServiceProvided = analysis.ServiceProvided,
                Expense = expense
            };
            _repository.AddNewBudgetAndDocument(expense, document);
            return analysis;
        }

        private static string DetermineDocumentType(string text)
        {
            if (Regex.IsMatch(text, @"\bINVOICE\b", RegexOptions.IgnoreCase))
                return "Invoice";
            if (Regex.IsMatch(text, @"\bRECEIPT\b", RegexOptions.IgnoreCase))
                return "Receipt";
            return "Unknown";
        }

        private static string ExtractInvoiceNumber(string text)
        {
            string[] patterns = {
            @"(?:INVOICE\s+#|Invoice\s+#|INVOICE NO[.:]+|Invoice No[.:]+)\s*([A-Za-z0-9\-_]+)",
            @"(?:Receipt\s+#|RECEIPT\s+#|Receipt No[.:]+|RECEIPT NO[.:]+)\s*([A-Za-z0-9\-_]+)",
            @"(?:Document|Order|Transaction)\s+(?:#|No[.:]+|Number[.:]+)\s*([A-Za-z0-9\-_]+)",
            @"(?:INV|REC)#\s*([A-Za-z0-9\-_]+)",
            @"#\s*([A-Za-z0-9\-_]+)"
        };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }

            return "Not Found";
        }

        private static string ExtractTransactionDate(string text)
        {
            string[] patterns = {
            @"(?:INVOICE\s+DATE|Invoice\s+Date|DATE)\s*(\d{1,2}/\d{1,2}/\d{2,4})",
            @"(?:Transaction\s+Date|Date)\s*(\d{1,2}/\d{1,2}/\d{2,4})",
            @"(\d{1,2}/\d{1,2}/\d{2,4})"
        };

            string[] dateFormats = { "dd/MM/yyyy", "d/M/yyyy", "M/d/yy", "MM/dd/yy" };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string rawDate = match.Groups[1].Value.Trim();
                    if (DateTime.TryParseExact(rawDate, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                    {
                        return parsedDate.ToString("dd/MM/yyyy");
                    }
                }
            }

            return "Not Found";
        }

        private static string ExtractCompanyName(string text)
        {
            var match = Regex.Match(text, @"^([A-Z][\w\s&\-,.]+?)(?=\s*\d{4}\s*[A-Z][a-z]+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : "Not Found";
        }

        private static void ExtractAmounts(string text, DocumentAnalysisResult result)
        {
            var subtotalMatch = Regex.Match(text, @"Subtotal\s*\$?\s*(\d+\.\d{2})", RegexOptions.IgnoreCase);
            result.AmountBeforeVAT = subtotalMatch.Success ? subtotalMatch.Groups[1].Value : "0";

            var totalMatch = Regex.Match(text, @"\b(?<!Sub)(TOTAL DUE|TOTAL)\b\s*\$?\s*(\d+\.\d{2})", RegexOptions.IgnoreCase);
            result.AmountAfterVAT = totalMatch.Success ? totalMatch.Groups[2].Value : "0";
        }

        private static string ExtractServiceProvided(string text)
        {
            var serviceMatches = Regex.Matches(text, @"(?:Q[TYry]+\s*\d+\s*)([A-Za-z0-9\s\-,.]+?)(?=\s*\d+\.\d{2})", RegexOptions.IgnoreCase);
            var services = new List<string>();
            foreach (Match match in serviceMatches)
            {
                if (match.Success)
            {
                    services.Add(match.Groups[1].Value.Trim());
                }
            }
            return services.Count > 0 ? string.Join("; ", services) : "Not Found";
        }

        private static string ExtractResellerNumber(string text)
        {
            var match = Regex.Match(text, @"Reseller(?:Number|No|#)?\s*[:\-]\s*(\w+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : "Not Found";
        }

        public DocumentAnalysisResult AnalyzeText(string documentText)
        {
            // Normalize the text: replace multiple whitespace with a single space
            documentText = Regex.Replace(documentText, @"\s+", " ").Trim();

            var result = new DocumentAnalysisResult();

            // Determine document type
            result.DocumentType = DetermineDocumentType(documentText);

            // Extract invoice number
            result.InvoiceNumber = ExtractInvoiceNumber(documentText);

            // Extract transaction date
            result.TransactionDate = ExtractTransactionDate(documentText);

            // Extract company name
            result.CompanyName = ExtractCompanyName(documentText);

            // Extract amounts
            ExtractAmounts(documentText, result);

            // Extract service provided
            result.ServiceProvided = ExtractServiceProvided(documentText);

            // Extract reseller number
            result.ResellerNumber = ExtractResellerNumber(documentText);

            return result;
        }

        //filters
        public async Task<List<Expense>> FilterExpenses(string? companyName, decimal? amountMin, decimal? amountMax, DateTime? startDate, DateTime? endDate, int? category, int clientId)
        {
            var allExpenses = await GetAllExpensesFromUserAsync(clientId);

            if (!string.IsNullOrEmpty(companyName))
            {
                allExpenses = allExpenses
                    .Where(e => !string.IsNullOrEmpty(e.CompanyName) &&
                                e.CompanyName.Contains(companyName, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (amountMin.HasValue || amountMax.HasValue)
            {
                if (amountMin.HasValue && amountMax.HasValue && amountMin > amountMax)
                    (amountMin, amountMax) = (amountMax, amountMin);

                allExpenses = allExpenses
                    .Where(e =>
                        (!amountMin.HasValue || e.AmountAfterVAT >= amountMin) &&
                        (!amountMax.HasValue || e.AmountAfterVAT <= amountMax)
                    ).ToList();
            }

            if (startDate.HasValue && endDate.HasValue)
            {
                if (startDate > endDate)
                    (startDate, endDate) = (endDate, startDate); // swap if out of order

                allExpenses = allExpenses
                    .Where(e => e.TransactionTime.Date >= startDate.Value.Date &&
                                e.TransactionTime.Date <= endDate.Value.Date)
                    .ToList();
            }
            else if (startDate.HasValue)
            {
                allExpenses = allExpenses
                    .Where(e => e.TransactionTime.Date >= startDate.Value.Date)
                    .ToList();
            }
            else if (endDate.HasValue)
            {
                allExpenses = allExpenses
                    .Where(e => e.TransactionTime.Date <= endDate.Value.Date)
                    .ToList();
            }


            if (category.HasValue)
            {
                allExpenses = allExpenses
                    .Where(e => (int)e.Category == category.Value).ToList();
            }
            return allExpenses;
        }

        public bool IsSupportedFileType(string type)
        {
            return type == "application/pdf" || type.StartsWith("image/");
        }

        public async Task<string> ExtractTextAsync(IFormFile file)
        {
            if (file.ContentType == "application/pdf")
            {
                using var pdf = UglyToad.PdfPig.PdfDocument.Open(file.OpenReadStream());

                var sb = new StringBuilder();
                foreach (var page in pdf.GetPages())
                {
                    sb.AppendLine(page.Text);
                }
                return sb.ToString();
            }
            return ExtractTextFromImage(file.OpenReadStream());


        }
        public async Task<Expense> GetExpenseByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task UpdateExpenseAsync(Expense expense)
        {
            await _repository.UpdateAsync(expense);
        }

    }
}

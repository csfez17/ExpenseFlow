namespace ExpenseFlow.Models
{

    public class DocumentAnalysisResult
    {
        public string DocumentType { get; set; }
        public string AmountBeforeVAT { get; set; }
        public string AmountAfterVAT { get; set; }
        public string TransactionDate { get; set; }
        public string CompanyName { get; set; }
        public string ResellerNumber { get; set; }
        public string ServiceProvided { get; set; }
        public string InvoiceNumber { get; set; }
    }

}

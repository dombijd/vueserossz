namespace GlosterIktato.API.DTOs.Dataxo
{
    /// <summary>
    /// Dataxo submit válasz
    /// </summary>
    public class DataxoSubmitResponse
    {
        public string TransactionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "queued", "processing"
        public DateTime SubmittedAt { get; set; }
    }

    /// <summary>
    /// Dataxo státusz lekérés válasz
    /// </summary>
    public class DataxoStatusResponse
    {
        public string TransactionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "processing", "success", "failed"
        public DataxoInvoiceData? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>
    /// Dataxo által kiolvasott számlaadatok
    /// </summary>
    public class DataxoInvoiceData
    {
        public string? InvoiceNumber { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? PerformanceDate { get; set; }
        public DateTime? PaymentDeadline { get; set; }
        public decimal? GrossAmount { get; set; }
        public string? Currency { get; set; }
        public string? SupplierName { get; set; }
        public string? SupplierTaxNumber { get; set; }
        public string? SupplierAddress { get; set; }
    }

    /// <summary>
    /// API válasz wrapper (frontend számára)
    /// </summary>
    public class DataxoStatusDto
    {
        public string Status { get; set; } = string.Empty; // processing, success, failed
        public DataxoInvoiceData? Data { get; set; }
        public string? Message { get; set; }
    }
}
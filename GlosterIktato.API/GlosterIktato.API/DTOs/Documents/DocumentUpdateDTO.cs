namespace GlosterIktato.API.DTOs.Documents
{
    public class DocumentUpdateDto
    {
        // Számla specifikus mezők
        public string? InvoiceNumber { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? PerformanceDate { get; set; }
        public DateTime? PaymentDeadline { get; set; }
        public decimal? GrossAmount { get; set; }
        public string? Currency { get; set; }

        // Supplier
        public int? SupplierId { get; set; }

        // BC mezők (később)
        public string? CostCenter { get; set; }
        public string? GptCode { get; set; }
        public string? BusinessUnit { get; set; }
        public string? Project { get; set; }
        public string? Employee { get; set; }
    }
}
namespace GlosterIktato.API.DTOs.Documents
{
    public class DocumentResponseDto
    {
        public int Id { get; set; }
        public string ArchiveNumber { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        // Számla specifikus mezők
        public string? InvoiceNumber { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? PerformanceDate { get; set; }
        public DateTime? PaymentDeadline { get; set; }
        public decimal? GrossAmount { get; set; }
        public string? Currency { get; set; }

        // Kapcsolatok
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;

        public int DocumentTypeId { get; set; }
        public string DocumentTypeName { get; set; } = string.Empty;
        public string DocumentTypeCode { get; set; } = string.Empty;

        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }

        public int CreatedByUserId { get; set; }
        public string CreatedByName { get; set; } = string.Empty;

        public int? AssignedToUserId { get; set; }
        public string? AssignedToName { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}

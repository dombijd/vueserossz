namespace GlosterIktato.API.DTOs.Documents
{
    /// <summary>
    /// Advanced search parameters for documents
    /// </summary>
    public class DocumentSearchDto
    {
        // Basic filters
        public int? CompanyId { get; set; }
        public int? SupplierId { get; set; }
        public int? DocumentTypeId { get; set; }

        // Status (multi-select - comma-separated)
        public string? Status { get; set; }

        // User assignment
        public int? AssignedToUserId { get; set; }

        // Date ranges
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public DateTime? IssueDateFrom { get; set; }
        public DateTime? IssueDateTo { get; set; }
        public DateTime? PaymentDeadlineFrom { get; set; }
        public DateTime? PaymentDeadlineTo { get; set; }

        // Amount filters
        public decimal? AmountFrom { get; set; }
        public decimal? AmountTo { get; set; }
        public string? Currency { get; set; }

        // Text search
        public string? InvoiceNumber { get; set; }
        public string? ArchiveNumber { get; set; }
        public string? FullTextSearch { get; set; } // Search in InvoiceNumber and Comments

        // Permission filter
        public bool? HasPermission { get; set; } // Only documents user has read permission for

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Sorting
        public string? SortBy { get; set; } // Field name to sort by
        public string? SortOrder { get; set; } = "desc"; // "asc" or "desc"
    }
}


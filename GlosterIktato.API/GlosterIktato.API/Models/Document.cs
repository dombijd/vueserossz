namespace GlosterIktato.API.Models
{
    public class Document
    {
        public int Id { get; set; }

        // Alapadatok
        public string ArchiveNumber { get; set; } = string.Empty; // Iktatószám (GeneratedRegistrationNumber helyett)
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty; // SharePoint path

        // Státusz és workflow
        public string Status { get; set; } = "Draft"; // Draft, PendingApproval, Accountant, Done, Rejected

        // Számla specifikus mezők (nullable, mert TIG/Szerződés/Egyéb nem használja)
        public string? InvoiceNumber { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? PerformanceDate { get; set; }
        public DateTime? PaymentDeadline { get; set; }
        public decimal? GrossAmount { get; set; }
        public string? Currency { get; set; } // HUF, EUR, USD

        // Dataxo integráció
        public string? DataxoTransactionId { get; set; }
        public string? DataxoStatus { get; set; } // null, Processing, Success, Failed
        public DateTime? DataxoSubmittedAt { get; set; }
        public DateTime? DataxoCompletedAt { get; set; }

        // Business Central integráció
        public string? BcInvoiceId { get; set; }
        public DateTime? BcPushedAt { get; set; }
        public string? BcStatus { get; set; } // null, Pending, Success, Failed

        // BC Master data (kategorizálás)
        public string? CostCenter { get; set; }
        public string? GptCode { get; set; }
        public string? BusinessUnit { get; set; }
        public string? Project { get; set; }
        public string? Employee { get; set; }

        // Kapcsolatok
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        public int DocumentTypeId { get; set; }
        public DocumentType DocumentType { get; set; } = null!;

        public int? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        // Workflow
        public int CreatedByUserId { get; set; }
        public User CreatedBy { get; set; } = null!;

        public int? AssignedToUserId { get; set; }
        public User? AssignedTo { get; set; }

        public int? ModifiedByUserId { get; set; }
        public User? ModifiedBy { get; set; }

        // Időbélyegek
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }

        // Navigációs propertyк
        public ICollection<DocumentHistory> History { get; set; } = new List<DocumentHistory>();

        /// <summary>
        /// Kapcsolatok ahol ez a dokumentum a fő dokumentum
        /// </summary>
        public ICollection<DocumentRelation> DocumentRelations { get; set; } = new List<DocumentRelation>();

        /// <summary>
        /// Kapcsolatok ahol ez a dokumentum a kapcsolódó dokumentum
        /// </summary>
        public ICollection<DocumentRelation> RelatedToDocuments { get; set; } = new List<DocumentRelation>();
    }
}
namespace GlosterIktato.API.Models
{
    public class Document
    {
        public int Id { get; set; }
        public string RegistrationNumber { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Status { get; set; } = "Vázlat";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }

        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        public int DocumentTypeId { get; set; }
        public DocumentType DocumentType { get; set; } = null!;

        public int? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        public int CreatedByUserId { get; set; }
        public User CreatedBy { get; set; } = null!;

        public int? AssignedToUserId { get; set; }
        public User? AssignedTo { get; set; }
    }
}
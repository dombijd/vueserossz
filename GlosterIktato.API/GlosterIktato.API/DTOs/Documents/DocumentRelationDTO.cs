using System.ComponentModel.DataAnnotations;

namespace GlosterIktato.API.DTOs.Documents
{
    /// <summary>
    /// Dokumentumok közötti kapcsolat létrehozása
    /// </summary>
    public class CreateDocumentRelationDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int DocumentId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int RelatedDocumentId { get; set; }

        [StringLength(50)]
        public string? RelationType { get; set; }

        [StringLength(500)]
        public string? Comment { get; set; }
    }

    /// <summary>
    /// Dokumentum kapcsolat válasz DTO
    /// </summary>
    public class DocumentRelationDto
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public string DocumentArchiveNumber { get; set; } = string.Empty;
        public int RelatedDocumentId { get; set; }
        public string RelatedDocumentArchiveNumber { get; set; } = string.Empty;
        public string? RelationType { get; set; }
        public string? Comment { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
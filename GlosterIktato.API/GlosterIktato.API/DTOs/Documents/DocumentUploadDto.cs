namespace GlosterIktato.API.DTOs.Documents
{
    public class DocumentUploadDto
    {
        public int CompanyId { get; set; }
        public int DocumentTypeId { get; set; }
        public IFormFile File { get; set; } = null!;
    }
}

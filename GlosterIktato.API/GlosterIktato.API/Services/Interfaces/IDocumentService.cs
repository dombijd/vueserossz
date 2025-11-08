using GlosterIktato.API.DTOs.Documents;

namespace GlosterIktato.API.Services.Interfaces
{
    public interface IDocumentService
    {
        Task<DocumentResponseDto?> UploadDocumentAsync(DocumentUploadDto dto, int currentUserId);
        Task<List<DocumentResponseDto>> GetMyTasksAsync(int currentUserId);
        Task<DocumentDetailDto?> GetDocumentByIdAsync(int documentId, int currentUserId);
    }
}

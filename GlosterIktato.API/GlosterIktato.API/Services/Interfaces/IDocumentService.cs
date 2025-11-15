using GlosterIktato.API.DTOs.Documents;

namespace GlosterIktato.API.Services.Interfaces
{
    public interface IDocumentService
    {
        Task<DocumentResponseDto?> UploadDocumentAsync(DocumentUploadDto dto, int currentUserId);
        Task<List<DocumentResponseDto>> GetMyTasksAsync(int currentUserId);
        Task<DocumentDetailDto?> GetDocumentByIdAsync(int documentId, int currentUserId);
        Task<DocumentDetailDto?> UpdateDocumentAsync(int documentId, DocumentUpdateDto dto, int currentUserId);
        Task<Stream?> DownloadDocumentAsync(int documentId, int currentUserId);
        Task<PaginatedResult<DocumentResponseDto>> GetDocumentsAsync(int userId, int? companyId, string? status, int? assignedToUserId, int page, int pageSize);
        Task<PaginatedResult<DocumentResponseDto>> SearchDocumentsAsync(int userId, DocumentSearchDto searchDto);
    }
}

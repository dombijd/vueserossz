using GlosterIktato.API.DTOs.Documents;

namespace GlosterIktato.API.Services.Interfaces
{
    public interface IDocumentRelationService
    {
        /// <summary>
        /// Dokumentum összes kapcsolatának lekérése (mindkét irány)
        /// </summary>
        Task<List<DocumentRelationDto>> GetDocumentRelationsAsync(int documentId, int currentUserId);

        /// <summary>
        /// Kapcsolat létrehozása két dokumentum között
        /// </summary>
        Task<DocumentRelationDto?> CreateRelationAsync(CreateDocumentRelationDto dto, int currentUserId);

        /// <summary>
        /// Kapcsolat törlése
        /// </summary>
        Task<bool> DeleteRelationAsync(int relationId, int currentUserId);

        /// <summary>
        /// Dokumentum összes kapcsolódó dokumentumának ID-jainak lekérése
        /// (includeRelated funkciónál használható)
        /// </summary>
        Task<List<int>> GetRelatedDocumentIdsAsync(int documentId, int currentUserId);
    }
}
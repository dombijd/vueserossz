using GlosterIktato.API.DTOs.Workflow;

namespace GlosterIktato.API.Services.Interfaces
{
    public interface IWorkflowService
    {
        /// <summary>
        /// Dokumentum továbbléptetése a következő státuszra
        /// - State machine logic
        /// - Értékhatár ellenőrzés (ElevatedApproval skip vagy sem)
        /// - Auto-assign vagy manual assign
        /// - History log
        /// </summary>
        Task<WorkflowActionResultDto> AdvanceDocumentAsync(int documentId, WorkflowAdvanceDto dto, int currentUserId);

        /// <summary>
        /// Dokumentum elutasítása
        /// - Státusz → Rejected
        /// - Auto-comment with reason
        /// - History log
        /// </summary>
        Task<WorkflowActionResultDto> RejectDocumentAsync(int documentId, WorkflowRejectDto dto, int currentUserId);

        /// <summary>
        /// Dokumentum delegálása másik felhasználónak
        /// - AssignedToUserId frissítés
        /// - History log
        /// </summary>
        Task<WorkflowActionResultDto> DelegateDocumentAsync(int documentId, WorkflowDelegateDto dto, int currentUserId);

        /// <summary>
        /// Dokumentum visszaléptetése az előző státuszba
        /// - Előző státusz meghatározása
        /// - Auto-assign vagy manual assign
        /// - History log
        /// </summary>
        Task<WorkflowActionResultDto> StepBackDocumentAsync(int documentId, WorkflowAdvanceDto dto, int currentUserId);

        /// <summary>
        /// Következő státusz meghatározása értékhatár logikával
        /// </summary>
        Task<string> DetermineNextStatusAsync(int documentId, string currentStatus);

        /// <summary>
        /// Előző státusz meghatározása step back művelethez
        /// </summary>
        Task<string> DeterminePreviousStatusAsync(int documentId, string currentStatus);
    }
}
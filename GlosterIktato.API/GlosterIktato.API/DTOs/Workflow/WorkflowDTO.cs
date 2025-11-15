using System.ComponentModel.DataAnnotations;

namespace GlosterIktato.API.DTOs.Workflow
{
    /// <summary>
    /// Workflow advance (továbbléptetés) request
    /// </summary>
    public class WorkflowAdvanceDto
    {
        /// <summary>
        /// Kinek legyen hozzárendelve a következő lépésben (opcionális)
        /// Ha nincs megadva, akkor auto-assign történik szerepkör alapján
        /// </summary>
        public int? AssignToUserId { get; set; }

        /// <summary>
        /// Megjegyzés a továbbléptetéshez (opcionális)
        /// </summary>
        [StringLength(500)]
        public string? Comment { get; set; }
    }

    /// <summary>
    /// Workflow reject (elutasítás) request
    /// </summary>
    public class WorkflowRejectDto
    {
        /// <summary>
        /// Elutasítás oka (KÖTELEZŐ!)
        /// </summary>
        [Required(ErrorMessage = "Elutasítás oka kötelező")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Elutasítás oka legalább 10 karakter kell legyen")]
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Workflow delegate (delegálás) request
    /// </summary>
    public class WorkflowDelegateDto
    {
        /// <summary>
        /// Kinek legyen delegálva (KÖTELEZŐ!)
        /// </summary>
        [Required(ErrorMessage = "Cél felhasználó megadása kötelező")]
        [Range(1, int.MaxValue, ErrorMessage = "Érvényes felhasználó ID szükséges")]
        public int TargetUserId { get; set; }

        /// <summary>
        /// Megjegyzés a delegáláshoz (opcionális)
        /// </summary>
        [StringLength(500)]
        public string? Comment { get; set; }
    }

    /// <summary>
    /// Workflow action result response
    /// </summary>
    public class WorkflowActionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? NewStatus { get; set; }
        public string? NewStatusDisplay { get; set; }
        public int? AssignedToUserId { get; set; }
        public string? AssignedToUserName { get; set; }
    }
}
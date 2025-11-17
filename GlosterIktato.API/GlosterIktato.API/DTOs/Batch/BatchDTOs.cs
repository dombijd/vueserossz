namespace GlosterIktato.API.DTOs.Batch
{
    /// <summary>
    /// Batch assign DTO
    /// </summary>
    public class BatchAssignDto
    {
        public List<int> DocumentIds { get; set; } = new();
        public int AssignToUserId { get; set; }
    }

    /// <summary>
    /// Batch update status DTO
    /// </summary>
    public class BatchUpdateStatusDto
    {
        public List<int> DocumentIds { get; set; } = new();
        public string NewStatus { get; set; } = string.Empty;
    }

    /// <summary>
    /// Batch delete DTO
    /// </summary>
    public class BatchDeleteDto
    {
        public List<int> DocumentIds { get; set; } = new();
    }

    /// <summary>
    /// Batch operation result DTO
    /// </summary>
    public class BatchOperationResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
    }
}
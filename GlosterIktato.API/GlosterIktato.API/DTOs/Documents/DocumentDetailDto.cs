namespace GlosterIktato.API.DTOs.Documents
{
    public class DocumentDetailDto : DocumentResponseDto
    {
        public string StoragePath { get; set; } = string.Empty;
        public List<DocumentHistoryDto> History { get; set; } = new();
        
        // Dataxo integráció mezők
        public string? DataxoTransactionId { get; set; }
        public string? DataxoStatus { get; set; } // null, Processing, Success, Failed
        public DateTime? DataxoSubmittedAt { get; set; }
        public DateTime? DataxoCompletedAt { get; set; }
    }

    public class DocumentHistoryDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? FieldName { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

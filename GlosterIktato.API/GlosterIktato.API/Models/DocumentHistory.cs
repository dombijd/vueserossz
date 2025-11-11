namespace GlosterIktato.API.Models
{
    public class DocumentHistory
    {
        public int Id { get; set; }

        public int DocumentId { get; set; }
        public Document Document { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public string Action { get; set; } = string.Empty; // Created, Modified, StatusChanged, Assigned, Rejected
        public string? FieldName { get; set; } // Melyik mező változott (ha releváns)
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string? Comment { get; set; } // Opcionális megjegyzés (pl. elutasítás oka)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

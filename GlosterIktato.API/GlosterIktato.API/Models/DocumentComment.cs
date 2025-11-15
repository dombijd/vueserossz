namespace GlosterIktato.API.Models
{
    /// <summary>
    /// Dokumentumhoz fűzött megjegyzések
    /// </summary>
    public class DocumentComment
    {
        public int Id { get; set; }

        // Foreign Keys
        public int DocumentId { get; set; }
        public Document Document { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        // Megjegyzés szövege
        public string Text { get; set; } = string.Empty;

        // Létrehozás időpontja
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
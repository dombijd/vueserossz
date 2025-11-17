using System.ComponentModel.DataAnnotations;

namespace GlosterIktato.API.DTOs.Comments
{
    public class CommentDto
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Új megjegyzés létrehozása DTO
    /// </summary>
    public class CommentCreateDto
    {
        [Required(ErrorMessage = "Megjegyzés szövege kötelező")]
        [StringLength(2000, MinimumLength = 1, ErrorMessage = "Megjegyzés hossza 1-2000 karakter között kell legyen")]
        public string Text { get; set; } = string.Empty;
    }
}

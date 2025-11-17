namespace GlosterIktato.API.Models
{
    /// <summary>
    /// Dokumentumok közötti kapcsolat (many-to-many)
    /// Példa: Számla + hozzá tartozó Teljesítésigazolás (TIG)
    /// </summary>
    public class DocumentRelation
    {
        public int Id { get; set; }

        /// <summary>
        /// Fő dokumentum (pl. Számla)
        /// </summary>
        public int DocumentId { get; set; }
        public Document Document { get; set; } = null!;

        /// <summary>
        /// Kapcsolódó dokumentum (pl. TIG, Szerződés)
        /// </summary>
        public int RelatedDocumentId { get; set; }
        public Document RelatedDocument { get; set; } = null!;

        /// <summary>
        /// Kapcsolat típusa (opcionális)
        /// </summary>
        public string? RelationType { get; set; } // Invoice-TIG, Contract-Invoice, stb.

        /// <summary>
        /// Megjegyzés a kapcsolatról (opcionális)
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Ki hozta létre a kapcsolatot
        /// </summary>
        public int CreatedByUserId { get; set; }
        public User CreatedBy { get; set; } = null!;

        /// <summary>
        /// Mikor hozták létre
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
namespace GlosterIktato.API.DTOs.Export
{
    public class ExportDocumentsDto
    {
        public List<int> DocumentIds { get; set; } = new();
    }

    public class ExportPdfZipDto
    {
        public List<int> DocumentIds { get; set; } = new();
        public bool IncludeRelated { get; set; } = false;
    }
}

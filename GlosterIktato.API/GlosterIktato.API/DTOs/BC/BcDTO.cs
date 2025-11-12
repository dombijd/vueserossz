namespace GlosterIktato.API.DTOs.BusinessCentral
{
    /// <summary>
    /// BC Master data - Költséghely (Cost Center)
    /// </summary>
    public class BcCostCenterDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool Blocked { get; set; }
    }

    /// <summary>
    /// BC Master data - Projekt
    /// </summary>
    public class BcProjectDto
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Open, Completed
    }

    /// <summary>
    /// BC Master data - GPT kód (General Product Type)
    /// </summary>
    public class BcGptCodeDto
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// BC Master data - Üzletág (Business Unit)
    /// </summary>
    public class BcBusinessUnitDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// BC Master data - Dolgozó (Employee)
    /// </summary>
    public class BcEmployeeDto
    {
        public string Code { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// BC Invoice push request
    /// </summary>
    public class BcInvoicePushRequest
    {
        public string DocumentNo { get; set; } = string.Empty; // ArchiveNumber
        public string VendorNo { get; set; } = string.Empty; // Supplier TaxNumber vagy Code
        public DateTime PostingDate { get; set; }
        public DateTime DocumentDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = "HUF";

        // Kategorizálás
        public string? CostCenter { get; set; }
        public string? GptCode { get; set; }
        public string? BusinessUnit { get; set; }
        public string? Project { get; set; }
        public string? Employee { get; set; }

        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// BC Invoice push response
    /// </summary>
    public class BcInvoicePushResponse
    {
        public string InvoiceId { get; set; } = string.Empty; // BC belső ID
        public string DocumentNo { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Posted, Pending, Failed
        public string? ErrorMessage { get; set; }
        public DateTime? PostedAt { get; set; }
    }

    /// <summary>
    /// BC API válasz wrapper (frontend számára)
    /// </summary>
    public class BcPushResultDto
    {
        public bool Success { get; set; }
        public string? BcInvoiceId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ErrorDetails { get; set; }
    }
}
namespace GlosterIktato.API.Models
{
    /// <summary>
    /// Dokumentum státuszok a workflow során
    /// </summary>
    public static class DocumentStatuses
    {
        public const string Draft = "Draft";
        public const string PendingApproval = "PendingApproval";
        public const string ElevatedApproval = "ElevatedApproval";
        public const string Accountant = "Accountant";
        public const string Done = "Done";
        public const string Rejected = "Rejected";
    }

    /// <summary>
    /// Workflow státusz helper metódusok
    /// </summary>
    public static class DocumentStatusExtensions
    {
        /// <summary>
        /// Magyar megjelenítési név
        /// </summary>
        public static string ToDisplayString(this string status)
        {
            return status switch
            {
                DocumentStatuses.Draft => "Vázlat",
                DocumentStatuses.PendingApproval => "Jóváhagyásra vár",
                DocumentStatuses.ElevatedApproval => "Emelt szintű jóváhagyásra vár",
                DocumentStatuses.Accountant => "Könyvelőnél",
                DocumentStatuses.Done => "Kész",
                DocumentStatuses.Rejected => "Elutasítva",
                _ => status
            };
        }

        /// <summary>
        /// Lehet-e továbbléptetni
        /// </summary>
        public static bool CanAdvance(this string status)
        {
            return status switch
            {
                DocumentStatuses.Draft => true,
                DocumentStatuses.PendingApproval => true,
                DocumentStatuses.ElevatedApproval => true,
                DocumentStatuses.Accountant => true,
                DocumentStatuses.Done => false,
                DocumentStatuses.Rejected => false,
                _ => false
            };
        }

        /// <summary>
        /// Lehet-e elutasítani
        /// </summary>
        public static bool CanReject(this string status)
        {
            return status != DocumentStatuses.Done && status != DocumentStatuses.Rejected;
        }

        /// <summary>
        /// Lehet-e delegálni
        /// </summary>
        public static bool CanDelegate(this string status)
        {
            return status != DocumentStatuses.Done && status != DocumentStatuses.Rejected;
        }

        /// <summary>
        /// Összes aktív státusz (Done és Rejected nélkül)
        /// </summary>
        public static string[] GetActiveStatuses()
        {
            return new[]
            {
                DocumentStatuses.Draft,
                DocumentStatuses.PendingApproval,
                DocumentStatuses.ElevatedApproval,
                DocumentStatuses.Accountant
            };
        }

        /// <summary>
        /// Összes lezárt státusz
        /// </summary>
        public static string[] GetClosedStatuses()
        {
            return new[]
            {
                DocumentStatuses.Done,
                DocumentStatuses.Rejected
            };
        }
    }
}
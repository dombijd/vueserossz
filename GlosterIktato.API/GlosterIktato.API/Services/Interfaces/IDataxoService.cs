using GlosterIktato.API.DTOs.Dataxo;

namespace GlosterIktato.API.Services.Interfaces
{
    /// <summary>
    /// Dataxo NAV számlaadat-kiolvasó integráció
    /// </summary>
    public interface IDataxoService
    {
        /// <summary>
        /// Számla PDF feltöltése Dataxo-ba OCR feldolgozásra
        /// </summary>
        /// <returns>TransactionId a későbbi státusz lekéréshez</returns>
        Task<string?> SubmitInvoiceAsync(Stream pdfStream, string fileName, int documentId);

        /// <summary>
        /// Dataxo feldolgozási státusz lekérése
        /// </summary>
        Task<DataxoStatusResponse> GetInvoiceDataAsync(string transactionId);

        /// <summary>
        /// Background job által hívott metódus: feldolgozás alatt lévő dokumentumok ellenőrzése
        /// </summary>
        Task ProcessPendingDocumentsAsync();
    }
}
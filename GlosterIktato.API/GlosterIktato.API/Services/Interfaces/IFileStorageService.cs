namespace GlosterIktato.API.Services.Interfaces
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Fájl feltöltése storage-ba
        /// </summary>
        Task<string> UploadFileAsync(Stream fileStream, string companyName, string supplierName, string fileName);

        /// <summary>
        /// Fájl letöltése storage-ból
        /// </summary>
        Task<Stream> DownloadFileAsync(string storagePath);

        /// <summary>
        /// Fájl törlése storage-ból
        /// </summary>
        Task<bool> DeleteFileAsync(string storagePath);

        /// <summary>
        /// Fájl átnevezése storage-ban
        /// </summary>
        Task<string> RenameFileAsync(string oldPath, string newFileName);
    }
}
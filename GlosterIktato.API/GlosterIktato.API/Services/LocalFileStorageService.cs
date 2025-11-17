using GlosterIktato.API.Services.Interfaces;

namespace GlosterIktato.API.Services
{
    /// <summary>
    /// Local file storage mock (SharePoint helyett fejlesztés alatt)
    /// </summary>
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _basePath;
        private readonly ILogger<LocalFileStorageService> _logger;

        public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
        {
            _basePath = configuration["FileStorage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Storage");
            _logger = logger;

            // Ensure base directory exists
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
                _logger.LogInformation("Created storage directory: {BasePath}", _basePath);
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string companyName, string supplierName, string fileName)
        {
            try
            {
                // Sanitize folder and file names
                var sanitizedCompany = SanitizeFileName(companyName);
                var sanitizedSupplier = SanitizeFileName(supplierName);
                var sanitizedFileName = SanitizeFileName(fileName);

                // Create folder structure: /Storage/{CompanyName}/{SupplierName}/
                var folderPath = Path.Combine(_basePath, sanitizedCompany, sanitizedSupplier);

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Full file path
                var filePath = Path.Combine(folderPath, sanitizedFileName);

                // Save file
                using (var fileStreamOutput = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    await fileStream.CopyToAsync(fileStreamOutput);
                }

                _logger.LogInformation("File uploaded successfully: {FilePath}", filePath);

                return filePath; // Return full path as storage identifier
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName}", fileName);
                throw;
            }
        }

        public async Task<Stream> DownloadFileAsync(string storagePath)
        {
            try
            {
                if (!File.Exists(storagePath))
                {
                    _logger.LogWarning("File not found: {StoragePath}", storagePath);
                    throw new FileNotFoundException("File not found", storagePath);
                }

                // Read file into memory stream
                var memoryStream = new MemoryStream();
                using (var fileStream = new FileStream(storagePath, FileMode.Open, FileAccess.Read))
                {
                    await fileStream.CopyToAsync(memoryStream);
                }

                memoryStream.Position = 0; // Reset stream position
                _logger.LogInformation("File downloaded successfully: {StoragePath}", storagePath);

                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file from {StoragePath}", storagePath);
                throw;
            }
        }

        public Task<bool> DeleteFileAsync(string storagePath)
        {
            try
            {
                if (File.Exists(storagePath))
                {
                    File.Delete(storagePath);
                    _logger.LogInformation("File deleted: {StoragePath}", storagePath);
                    return Task.FromResult(true);
                }

                _logger.LogWarning("File not found for deletion: {StoragePath}", storagePath);
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {StoragePath}", storagePath);
                throw;
            }
        }

        public Task<string> RenameFileAsync(string oldPath, string newFileName)
        {
            try
            {
                if (!File.Exists(oldPath))
                {
                    _logger.LogWarning("File not found for rename: {OldPath}", oldPath);
                    throw new FileNotFoundException("File not found", oldPath);
                }

                var directory = Path.GetDirectoryName(oldPath) ?? _basePath;
                var sanitizedNewFileName = SanitizeFileName(newFileName);
                var newPath = Path.Combine(directory, sanitizedNewFileName);

                // If file already exists at new path, append timestamp
                if (File.Exists(newPath))
                {
                    var fileNameWithoutExt = Path.GetFileNameWithoutExtension(sanitizedNewFileName);
                    var extension = Path.GetExtension(sanitizedNewFileName);
                    sanitizedNewFileName = $"{fileNameWithoutExt}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
                    newPath = Path.Combine(directory, sanitizedNewFileName);
                }

                File.Move(oldPath, newPath);
                _logger.LogInformation("File renamed from {OldPath} to {NewPath}", oldPath, newPath);

                return Task.FromResult(newPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renaming file from {OldPath} to {NewFileName}", oldPath, newFileName);
                throw;
            }
        }

        /// <summary>
        /// Remove invalid file name characters
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "Unknown";

            // Remove invalid characters: / \ : * ? " < > |
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

            // Also remove additional problematic characters
            sanitized = sanitized.Replace(" ", "_").Replace(".", "_");

            return string.IsNullOrWhiteSpace(sanitized) ? "Unknown" : sanitized;
        }
    }
}
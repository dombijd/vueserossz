using GlosterIktato.API.Data;
using GlosterIktato.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GlosterIktato.API.Services
{
    public class ArchiveNumberService : IArchiveNumberService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ArchiveNumberService> _logger;
        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        public ArchiveNumberService(ApplicationDbContext context, ILogger<ArchiveNumberService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Generál egyedi iktatószámot: {CompanyCode}-{DocType}-{YYMMDD}-{Seq}
        /// Példa: P-SZLA-250108-0001
        /// Ellenőrzi, hogy a generált szám egyedi-e, ha nem, újragenerálja
        /// </summary>
        public async Task<string> GenerateArchiveNumberAsync(int companyId, int documentTypeId)
        {
            await _semaphore.WaitAsync(); // Thread-safe generálás
            try
            {
                // 1. Cég és dokumentumtípus lekérése
                var company = await _context.Companies.FindAsync(companyId);
                var docType = await _context.DocumentTypes.FindAsync(documentTypeId);

                if (company == null || docType == null)
                {
                    throw new InvalidOperationException("Company or DocumentType not found");
                }

                // 2. Cégkód: első betű a cég nevéből (vagy custom mező, ha van)
                var companyCode = company.Name.Substring(0, 1).ToUpper();

                // 3. Dátum formázás: YYMMDD
                var dateStr = DateTime.UtcNow.ToString("yyMMdd");

                // 4. Mai napi szekvencia lekérése
                var today = DateTime.UtcNow.Date;
                var todayEnd = today.AddDays(1);

                var todayCount = await _context.Documents
                    .Where(d => d.CompanyId == companyId
                             && d.DocumentTypeId == documentTypeId
                             && d.CreatedAt >= today
                             && d.CreatedAt < todayEnd)
                    .CountAsync();

                var sequence = todayCount + 1;
                const int maxAttempts = 1000; // Safety limit to prevent infinite loops
                int attempts = 0;
                string archiveNumber;

                // 5. Iktatószám generálás és egyediség ellenőrzés
                do
                {
                    archiveNumber = $"{companyCode}-{docType.Code}-{dateStr}-{sequence:D4}";
                    
                    // Ellenőrzés, hogy létezik-e már ez az iktatószám
                    var exists = await _context.Documents
                        .AnyAsync(d => d.ArchiveNumber == archiveNumber);

                    if (!exists)
                    {
                        // Egyedi számot találtunk
                        _logger.LogInformation("Generated unique archive number: {ArchiveNumber}", archiveNumber);
                        return archiveNumber;
                    }

                    // Ha létezik, növeljük a szekvenciát és újra próbáljuk
                    sequence++;
                    attempts++;

                    if (attempts >= maxAttempts)
                    {
                        _logger.LogError("Failed to generate unique archive number after {Attempts} attempts", maxAttempts);
                        throw new InvalidOperationException($"Could not generate unique archive number after {maxAttempts} attempts");
                    }

                    _logger.LogWarning("Archive number {ArchiveNumber} already exists, trying next sequence: {Sequence}", 
                        archiveNumber, sequence);
                }
                while (attempts < maxAttempts);

                // This should never be reached due to the exception above, but compiler needs it
                throw new InvalidOperationException("Failed to generate unique archive number");
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
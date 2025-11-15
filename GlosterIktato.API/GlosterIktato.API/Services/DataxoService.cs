using GlosterIktato.API.Data;
using GlosterIktato.API.DTOs.Dataxo;
using GlosterIktato.API.Models;
using GlosterIktato.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GlosterIktato.API.Services
{
    /// <summary>
    /// Dataxo integráció (MOCK implementáció - később lecserélhető valódi API-ra)
    /// </summary>
    public class DataxoService : IDataxoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DataxoService> _logger;
        private readonly HttpClient _httpClient;

        // Mock: Transaction státuszok tárolása memóriában
        private static readonly Dictionary<string, MockTransaction> _mockTransactions = new();
        private static readonly Random _random = new(42);

        public DataxoService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<DataxoService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("DataxoClient");
        }

        /// <summary>
        /// PDF feltöltése Dataxo-ba (MOCK)
        /// </summary>
        public async Task<string?> SubmitInvoiceAsync(Stream pdfStream, string fileName, int documentId)
        {
            try
            {
                _logger.LogInformation("Submitting invoice to Dataxo (MOCK): DocumentId={DocumentId}, FileName={FileName}",
                    documentId, fileName);

                // MOCK: Generálunk egy TransactionId-t
                var transactionId = $"DTX-{DateTime.UtcNow:yyyyMMddHHmmss}-{_random.Next(1000, 9999)}";

                // MOCK: Létrehozzuk a mock transaction-t memóriában
                var mockTransaction = new MockTransaction
                {
                    TransactionId = transactionId,
                    DocumentId = documentId,
                    FileName = fileName,
                    Status = "processing",
                    SubmittedAt = DateTime.UtcNow,
                    // Mock: Véletlenszerű idő után lesz kész (10-60 sec)
                    EstimatedCompletionAt = DateTime.UtcNow.AddSeconds(_random.Next(10, 60))
                };

                _mockTransactions[transactionId] = mockTransaction;

                _logger.LogInformation("Invoice submitted successfully (MOCK): TransactionId={TransactionId}", transactionId);

                return transactionId;

                // TODO: VALÓDI IMPLEMENTÁCIÓ:
                /*
                var baseUrl = _configuration["Dataxo:BaseUrl"];
                var apiKey = _configuration["Dataxo:ApiKey"];

                using var content = new MultipartFormDataContent();
                content.Add(new StreamContent(pdfStream), "file", fileName);
                content.Add(new StringContent(documentId.ToString()), "documentId");

                _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                var response = await _httpClient.PostAsync($"{baseUrl}/api/invoices/submit", content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Dataxo submit failed: {StatusCode}", response.StatusCode);
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<DataxoSubmitResponse>();
                return result?.TransactionId;
                */
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting invoice to Dataxo for DocumentId={DocumentId}", documentId);
                return null;
            }
        }

        /// <summary>
        /// Dataxo státusz lekérése (MOCK)
        /// </summary>
        public async Task<DataxoStatusResponse> GetInvoiceDataAsync(string transactionId)
        {
            try
            {
                _logger.LogInformation("Checking Dataxo status (MOCK): TransactionId={TransactionId}", transactionId);

                // MOCK: Megnézzük a memóriában tárolt transaction-t
                if (!_mockTransactions.TryGetValue(transactionId, out var mockTransaction))
                {
                    _logger.LogWarning("Transaction not found (MOCK): {TransactionId}", transactionId);
                    return new DataxoStatusResponse
                    {
                        TransactionId = transactionId,
                        Status = "failed",
                        ErrorMessage = "Transaction not found"
                    };
                }

                // MOCK: Ha még nem telt el az estimated completion time, akkor "processing"
                if (DateTime.UtcNow < mockTransaction.EstimatedCompletionAt)
                {
                    return new DataxoStatusResponse
                    {
                        TransactionId = transactionId,
                        Status = "processing"
                    };
                }

                // MOCK: 95% eséllyel "success", 5% eséllyel "failed"
                var isSuccess = _random.Next(100) < 95;

                if (!isSuccess)
                {
                    mockTransaction.Status = "failed";
                    return new DataxoStatusResponse
                    {
                        TransactionId = transactionId,
                        Status = "failed",
                        ErrorMessage = "OCR processing failed - image quality too low",
                        CompletedAt = DateTime.UtcNow
                    };
                }

                // MOCK: Sikeres feldolgozás - generálunk random számlaadatokat
                mockTransaction.Status = "success";

                var mockInvoiceData = await GenerateMockInvoiceDataAsync(mockTransaction.DocumentId);

                _logger.LogInformation("Dataxo processing completed successfully (MOCK): TransactionId={TransactionId}",
                    transactionId);

                return new DataxoStatusResponse
                {
                    TransactionId = transactionId,
                    Status = "success",
                    Data = mockInvoiceData,
                    CompletedAt = DateTime.UtcNow
                };

                // TODO: VALÓDI IMPLEMENTÁCIÓ:
                /*
                var baseUrl = _configuration["Dataxo:BaseUrl"];
                var apiKey = _configuration["Dataxo:ApiKey"];

                _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                var response = await _httpClient.GetAsync($"{baseUrl}/api/invoices/{transactionId}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Dataxo status check failed: {StatusCode}", response.StatusCode);
                    return new DataxoStatusResponse
                    {
                        TransactionId = transactionId,
                        Status = "failed",
                        ErrorMessage = "API request failed"
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<DataxoStatusResponse>();
                return result ?? new DataxoStatusResponse { TransactionId = transactionId, Status = "failed" };
                */
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Dataxo status for TransactionId={TransactionId}", transactionId);
                return new DataxoStatusResponse
                {
                    TransactionId = transactionId,
                    Status = "failed",
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Background job: feldolgozás alatt lévő dokumentumok ellenőrzése
        /// </summary>
        public async Task ProcessPendingDocumentsAsync()
        {
            try
            {
                // Keressük meg azokat a dokumentumokat, amik "Processing" státuszban vannak
                var processingDocuments = await _context.Documents
                    .Include(d => d.Supplier)
                    .Include(d => d.Company)
                    .Where(d => d.DataxoStatus == "Processing" && d.DataxoTransactionId != null)
                    .ToListAsync();

                if (!processingDocuments.Any())
                {
                    _logger.LogDebug("No pending Dataxo documents to process");
                    return;
                }

                _logger.LogInformation("Processing {Count} pending Dataxo documents", processingDocuments.Count);

                foreach (var document in processingDocuments)
                {
                    try
                    {
                        // Lekérjük a Dataxo státuszt
                        var statusResponse = await GetInvoiceDataAsync(document.DataxoTransactionId!);

                        if (statusResponse.Status == "processing")
                        {
                            // Még mindig feldolgozás alatt van
                            _logger.LogDebug("Document {DocumentId} still processing in Dataxo", document.Id);
                            continue;
                        }

                        if (statusResponse.Status == "failed")
                        {
                            // Sikertelen feldolgozás
                            document.DataxoStatus = "Failed";
                            document.DataxoCompletedAt = DateTime.UtcNow;
                            document.Status = "DataxoFailed";

                            // History bejegyzés
                            _context.DocumentHistories.Add(new DocumentHistory
                            {
                                DocumentId = document.Id,
                                UserId = document.CreatedByUserId,
                                Action = "DataxoFailed",
                                Comment = $"Dataxo feldolgozás sikertelen: {statusResponse.ErrorMessage}",
                                CreatedAt = DateTime.UtcNow
                            });

                            _logger.LogWarning("Dataxo processing failed for DocumentId={DocumentId}: {Error}",
                                document.Id, statusResponse.ErrorMessage);
                        }
                        else if (statusResponse.Status == "success" && statusResponse.Data != null)
                        {
                            // Sikeres feldolgozás - frissítjük a dokumentum adatait
                            var data = statusResponse.Data;

                            document.InvoiceNumber = data.InvoiceNumber;
                            document.IssueDate = data.IssueDate;
                            document.PerformanceDate = data.PerformanceDate;
                            document.PaymentDeadline = data.PaymentDeadline;
                            document.GrossAmount = data.GrossAmount;
                            document.Currency = data.Currency ?? "HUF";
                            document.DataxoStatus = "Success";
                            document.DataxoCompletedAt = DateTime.UtcNow;
                            document.ModifiedAt = DateTime.UtcNow;

                            // Szállító kezelés: megpróbáljuk megtalálni vagy létrehozni
                            if (!string.IsNullOrWhiteSpace(data.SupplierTaxNumber))
                            {
                                var supplier = await _context.Suppliers
                                    .FirstOrDefaultAsync(s => s.TaxNumber == data.SupplierTaxNumber);

                                if (supplier == null && !string.IsNullOrWhiteSpace(data.SupplierName))
                                {
                                    // Automatikusan létrehozzuk a szállítót
                                    supplier = new Supplier
                                    {
                                        Name = data.SupplierName,
                                        TaxNumber = data.SupplierTaxNumber,
                                        Address = data.SupplierAddress,
                                        IsActive = true,
                                        CreatedAt = DateTime.UtcNow
                                    };
                                    _context.Suppliers.Add(supplier);
                                    await _context.SaveChangesAsync(); // Save to get ID

                                    _logger.LogInformation("Auto-created supplier from Dataxo: {SupplierName} ({TaxNumber})",
                                        supplier.Name, supplier.TaxNumber);
                                }

                                if (supplier != null)
                                {
                                    document.SupplierId = supplier.Id;
                                }
                            }

                            // History bejegyzés
                            _context.DocumentHistories.Add(new DocumentHistory
                            {
                                DocumentId = document.Id,
                                UserId = document.CreatedByUserId,
                                Action = "DataxoSuccess",
                                Comment = "Dataxo feldolgozás sikeres - számlaadatok automatikusan kitöltve",
                                CreatedAt = DateTime.UtcNow
                            });

                            _logger.LogInformation("Dataxo processing completed for DocumentId={DocumentId}, InvoiceNumber={InvoiceNumber}",
                                document.Id, document.InvoiceNumber);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing Dataxo document {DocumentId}", document.Id);
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Processed {Count} pending Dataxo documents", processingDocuments.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessPendingDocumentsAsync");
            }
        }

        /// <summary>
        /// MOCK: Generál random számlaadatokat a dokumentum alapján
        /// </summary>
        private async Task<DataxoInvoiceData> GenerateMockInvoiceDataAsync(int documentId)
        {
            var document = await _context.Documents
                .Include(d => d.Supplier)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            var suppliers = await _context.Suppliers.Where(s => s.IsActive).ToListAsync();
            var randomSupplier = suppliers[_random.Next(suppliers.Count)];

            return new DataxoInvoiceData
            {
                InvoiceNumber = $"SZ-{DateTime.UtcNow.Year}-{_random.Next(1000, 9999)}",
                IssueDate = DateTime.UtcNow.AddDays(-_random.Next(1, 30)),
                PerformanceDate = DateTime.UtcNow.AddDays(-_random.Next(1, 15)),
                PaymentDeadline = DateTime.UtcNow.AddDays(_random.Next(15, 45)),
                GrossAmount = _random.Next(10000, 500000),
                Currency = _random.Next(100) < 80 ? "HUF" : "EUR",
                SupplierName = randomSupplier.Name,
                SupplierTaxNumber = randomSupplier.TaxNumber,
                SupplierAddress = randomSupplier.Address
            };
        }

        // Mock helper class
        private class MockTransaction
        {
            public string TransactionId { get; set; } = string.Empty;
            public int DocumentId { get; set; }
            public string FileName { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public DateTime SubmittedAt { get; set; }
            public DateTime EstimatedCompletionAt { get; set; }
        }
    }
}
using GlosterIktato.API.DTOs.BusinessCentral;
using GlosterIktato.API.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace GlosterIktato.API.Services
{
    /// <summary>
    /// Business Central integráció (MOCK implementáció - később lecserélhető valódi API-ra)
    /// </summary>
    public class BusinessCentralService : IBusinessCentralService
    {
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BusinessCentralService> _logger;
        private readonly HttpClient _httpClient;
        private static readonly TimeSpan _cacheDuration = TimeSpan.FromHours(1);
        private static readonly Random _random = new(42);

        public BusinessCentralService(
            IMemoryCache cache,
            IConfiguration configuration,
            ILogger<BusinessCentralService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _cache = cache;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("BusinessCentralClient");
        }

        /// <summary>
        /// Költséghelyek lekérése (cache-elt)
        /// </summary>
        public async Task<List<BcCostCenterDto>> GetCostCentersAsync(int companyId)
        {
            var cacheKey = $"BC_CostCenters_{companyId}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
                _logger.LogInformation("Fetching cost centers from BC (MOCK) for CompanyId={CompanyId}", companyId);

                // MOCK: Generált költséghelyek
                return await Task.FromResult(new List<BcCostCenterDto>
                {
                    new() { Code = "ADMIN", Name = "Adminisztráció", Blocked = false },
                    new() { Code = "IT", Name = "Informatika", Blocked = false },
                    new() { Code = "SALES", Name = "Értékesítés", Blocked = false },
                    new() { Code = "PROD", Name = "Gyártás", Blocked = false },
                    new() { Code = "HR", Name = "Humán Erőforrás", Blocked = false },
                    new() { Code = "FIN", Name = "Pénzügy", Blocked = false },
                    new() { Code = "MARK", Name = "Marketing", Blocked = false },
                    new() { Code = "LOG", Name = "Logisztika", Blocked = false },
                });

                // TODO: VALÓDI IMPLEMENTÁCIÓ
                /*
                var baseUrl = _configuration["BusinessCentral:BaseUrl"];
                var response = await _httpClient.GetAsync($"{baseUrl}/api/v2.0/companies({companyId})/costCenters");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("BC cost centers fetch failed: {StatusCode}", response.StatusCode);
                    return new List<BcCostCenterDto>();
                }

                var result = await response.Content.ReadFromJsonAsync<BcApiResponse<BcCostCenterDto>>();
                return result?.Value ?? new List<BcCostCenterDto>();
                */
            }) ?? new List<BcCostCenterDto>();
        }

        /// <summary>
        /// Projektek lekérése (cache-elt)
        /// </summary>
        public async Task<List<BcProjectDto>> GetProjectsAsync(int companyId)
        {
            var cacheKey = $"BC_Projects_{companyId}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
                _logger.LogInformation("Fetching projects from BC (MOCK) for CompanyId={CompanyId}", companyId);

                // MOCK: Generált projektek
                return await Task.FromResult(new List<BcProjectDto>
                {
                    new() { Code = "PRJ-001", Description = "ERP Rendszer Fejlesztés", Status = "Open" },
                    new() { Code = "PRJ-002", Description = "Raktár Bővítés", Status = "Open" },
                    new() { Code = "PRJ-003", Description = "Webshop Modernizáció", Status = "Open" },
                    new() { Code = "PRJ-004", Description = "Marketing Kampány 2025", Status = "Open" },
                    new() { Code = "PRJ-005", Description = "ISO Tanúsítvány", Status = "Completed" },
                });

                // TODO: VALÓDI IMPLEMENTÁCIÓ
            }) ?? new List<BcProjectDto>();
        }

        /// <summary>
        /// GPT kódok lekérése (cache-elt)
        /// </summary>
        public async Task<List<BcGptCodeDto>> GetGptCodesAsync(int companyId)
        {
            var cacheKey = $"BC_GptCodes_{companyId}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
                _logger.LogInformation("Fetching GPT codes from BC (MOCK) for CompanyId={CompanyId}", companyId);

                // MOCK: Generált GPT kódok
                return await Task.FromResult(new List<BcGptCodeDto>
                {
                    new() { Code = "MAT", Description = "Anyagköltség" },
                    new() { Code = "SVC", Description = "Szolgáltatás" },
                    new() { Code = "EQP", Description = "Eszközbeszerzés" },
                    new() { Code = "TRV", Description = "Utazási költség" },
                    new() { Code = "UTL", Description = "Rezsi" },
                    new() { Code = "ADV", Description = "Reklám és marketing" },
                    new() { Code = "CON", Description = "Tanácsadás" },
                    new() { Code = "OTH", Description = "Egyéb" },
                });

                // TODO: VALÓDI IMPLEMENTÁCIÓ
            }) ?? new List<BcGptCodeDto>();
        }

        /// <summary>
        /// Üzletágak lekérése (cache-elt)
        /// </summary>
        public async Task<List<BcBusinessUnitDto>> GetBusinessUnitsAsync(int companyId)
        {
            var cacheKey = $"BC_BusinessUnits_{companyId}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
                _logger.LogInformation("Fetching business units from BC (MOCK) for CompanyId={CompanyId}", companyId);

                // MOCK: Generált üzletágak
                return await Task.FromResult(new List<BcBusinessUnitDto>
                {
                    new() { Code = "RETAIL", Name = "Kiskereskedelmi Üzletág" },
                    new() { Code = "WHOLE", Name = "Nagykereskedelmi Üzletág" },
                    new() { Code = "ONLINE", Name = "Online Üzletág" },
                    new() { Code = "B2B", Name = "Vállalati Értékesítés" },
                    new() { Code = "EXPORT", Name = "Export Üzletág" },
                });

                // TODO: VALÓDI IMPLEMENTÁCIÓ
            }) ?? new List<BcBusinessUnitDto>();
        }

        /// <summary>
        /// Dolgozók lekérése (cache-elt)
        /// </summary>
        public async Task<List<BcEmployeeDto>> GetEmployeesAsync(int companyId)
        {
            var cacheKey = $"BC_Employees_{companyId}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _cacheDuration;
                _logger.LogInformation("Fetching employees from BC (MOCK) for CompanyId={CompanyId}", companyId);

                // MOCK: Generált dolgozók
                return await Task.FromResult(new List<BcEmployeeDto>
                {
                    new() { Code = "EMP001", FullName = "Kovács János", Email = "janos.kovacs@gloster.hu" },
                    new() { Code = "EMP002", FullName = "Nagy Éva", Email = "eva.nagy@gloster.hu" },
                    new() { Code = "EMP003", FullName = "Tóth Péter", Email = "peter.toth@gloster.hu" },
                    new() { Code = "EMP004", FullName = "Szabó Anna", Email = "anna.szabo@gloster.hu" },
                    new() { Code = "EMP005", FullName = "Kiss Gábor", Email = "gabor.kiss@gloster.hu" },
                    new() { Code = "EMP006", FullName = "Horváth Katalin", Email = "katalin.horvath@gloster.hu" },
                });

                // TODO: VALÓDI IMPLEMENTÁCIÓ
            }) ?? new List<BcEmployeeDto>();
        }

        /// <summary>
        /// Számla push to Business Central
        /// </summary>
        public async Task<BcInvoicePushResponse> PushInvoiceAsync(BcInvoicePushRequest request)
        {
            try
            {
                _logger.LogInformation("Pushing invoice to BC (MOCK): DocumentNo={DocumentNo}, Amount={Amount}",
                    request.DocumentNo, request.Amount);

                // MOCK: Véletlenszerű siker/fail (98% success)
                await Task.Delay(_random.Next(500, 2000)); // Simulate API call

                var isSuccess = _random.Next(100) < 98;

                if (!isSuccess)
                {
                    _logger.LogWarning("BC invoice push failed (MOCK): DocumentNo={DocumentNo}", request.DocumentNo);
                    return new BcInvoicePushResponse
                    {
                        DocumentNo = request.DocumentNo,
                        Status = "Failed",
                        ErrorMessage = "BC API timeout - please retry"
                    };
                }

                // MOCK: Sikeres push
                var bcInvoiceId = $"BC-INV-{DateTime.UtcNow:yyyyMMddHHmmss}-{_random.Next(1000, 9999)}";

                _logger.LogInformation("Invoice pushed successfully to BC (MOCK): BcInvoiceId={BcInvoiceId}", bcInvoiceId);

                return new BcInvoicePushResponse
                {
                    InvoiceId = bcInvoiceId,
                    DocumentNo = request.DocumentNo,
                    Status = "Posted",
                    PostedAt = DateTime.UtcNow
                };

                // TODO: VALÓDI IMPLEMENTÁCIÓ
                /*
                var baseUrl = _configuration["BusinessCentral:BaseUrl"];
                var companyId = _configuration["BusinessCentral:CompanyId"];
                
                var response = await _httpClient.PostAsJsonAsync(
                    $"{baseUrl}/api/v2.0/companies({companyId})/purchaseInvoices",
                    request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("BC invoice push failed: {StatusCode}, {Error}", response.StatusCode, error);
                    
                    return new BcInvoicePushResponse
                    {
                        DocumentNo = request.DocumentNo,
                        Status = "Failed",
                        ErrorMessage = $"BC API error: {response.StatusCode}"
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<BcInvoicePushResponse>();
                return result ?? new BcInvoicePushResponse { Status = "Failed", ErrorMessage = "No response from BC" };
                */
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pushing invoice to BC: DocumentNo={DocumentNo}", request.DocumentNo);
                return new BcInvoicePushResponse
                {
                    DocumentNo = request.DocumentNo,
                    Status = "Failed",
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Cache törlése (admin funkció)
        /// </summary>
        public void ClearCache()
        {
            _logger.LogInformation("BC cache cleared manually");
            // Note: IMemoryCache nem támogatja a teljes flush-t
            // Egyedi kulcsok alapján kell törölni, vagy Memory Cache Invalidation pattern használata
        }
    }
}
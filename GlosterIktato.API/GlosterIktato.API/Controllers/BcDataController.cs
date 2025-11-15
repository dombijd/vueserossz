using GlosterIktato.API.DTOs.BusinessCentral;
using GlosterIktato.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GlosterIktato.API.Controllers
{
    /// <summary>
    /// Business Central master data API (frontend számára)
    /// </summary>
    [Route("api/bc-data")]
    [ApiController]
    [Authorize]
    public class BcDataController : ControllerBase
    {
        private readonly IBusinessCentralService _bcService;
        private readonly ILogger<BcDataController> _logger;

        public BcDataController(
            IBusinessCentralService bcService,
            ILogger<BcDataController> logger)
        {
            _bcService = bcService;
            _logger = logger;
        }

        /// <summary>
        /// Költséghelyek lekérése (cache-elt, 1 óra)
        /// GET /api/bc-data/cost-centers?companyId=1
        /// </summary>
        [HttpGet("cost-centers")]
        [ProducesResponseType(typeof(List<BcCostCenterDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCostCenters([FromQuery] int companyId)
        {
            try
            {
                if (companyId <= 0)
                    return BadRequest(new { message = "Invalid companyId" });

                var result = await _bcService.GetCostCentersAsync(companyId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching BC cost centers for CompanyId={CompanyId}", companyId);
                return StatusCode(500, new { message = "Hiba történt a költséghelyek lekérése során" });
            }
        }

        /// <summary>
        /// Projektek lekérése (cache-elt, 1 óra)
        /// GET /api/bc-data/projects?companyId=1
        /// </summary>
        [HttpGet("projects")]
        [ProducesResponseType(typeof(List<BcProjectDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProjects([FromQuery] int companyId)
        {
            try
            {
                if (companyId <= 0)
                    return BadRequest(new { message = "Invalid companyId" });

                var result = await _bcService.GetProjectsAsync(companyId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching BC projects for CompanyId={CompanyId}", companyId);
                return StatusCode(500, new { message = "Hiba történt a projektek lekérése során" });
            }
        }

        /// <summary>
        /// GPT kódok lekérése (cache-elt, 1 óra)
        /// GET /api/bc-data/gpt-codes?companyId=1
        /// </summary>
        [HttpGet("gpt-codes")]
        [ProducesResponseType(typeof(List<BcGptCodeDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetGptCodes([FromQuery] int companyId)
        {
            try
            {
                if (companyId <= 0)
                    return BadRequest(new { message = "Invalid companyId" });

                var result = await _bcService.GetGptCodesAsync(companyId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching BC GPT codes for CompanyId={CompanyId}", companyId);
                return StatusCode(500, new { message = "Hiba történt a GPT kódok lekérése során" });
            }
        }

        /// <summary>
        /// Üzletágak lekérése (cache-elt, 1 óra)
        /// GET /api/bc-data/business-units?companyId=1
        /// </summary>
        [HttpGet("business-units")]
        [ProducesResponseType(typeof(List<BcBusinessUnitDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBusinessUnits([FromQuery] int companyId)
        {
            try
            {
                if (companyId <= 0)
                    return BadRequest(new { message = "Invalid companyId" });

                var result = await _bcService.GetBusinessUnitsAsync(companyId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching BC business units for CompanyId={CompanyId}", companyId);
                return StatusCode(500, new { message = "Hiba történt az üzletágak lekérése során" });
            }
        }

        /// <summary>
        /// Dolgozók lekérése (cache-elt, 1 óra)
        /// GET /api/bc-data/employees?companyId=1
        /// </summary>
        [HttpGet("employees")]
        [ProducesResponseType(typeof(List<BcEmployeeDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEmployees([FromQuery] int companyId)
        {
            try
            {
                if (companyId <= 0)
                    return BadRequest(new { message = "Invalid companyId" });

                var result = await _bcService.GetEmployeesAsync(companyId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching BC employees for CompanyId={CompanyId}", companyId);
                return StatusCode(500, new { message = "Hiba történt a dolgozók lekérése során" });
            }
        }

        /// <summary>
        /// Cache törlése (admin funkció)
        /// POST /api/bc-data/clear-cache
        /// </summary>
        [HttpPost("clear-cache")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult ClearCache()
        {
            try
            {
                _bcService.ClearCache();
                _logger.LogInformation("BC cache cleared by admin");
                return Ok(new { message = "BC cache törölve" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing BC cache");
                return StatusCode(500, new { message = "Hiba történt a cache törlése során" });
            }
        }
    }
}
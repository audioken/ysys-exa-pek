using Microsoft.AspNetCore.Mvc;
using Senior.Models;
using Senior.Services;

namespace Senior.Controllers;

/// <summary>
/// Controller för CV-analys
/// Följer Clean Architecture: Tunn controller som endast hanterar HTTP och delegerar till service-lagret
/// Följer Single Responsibility Principle - ansvarar endast för HTTP-kommunikation
/// </summary>
[ApiController]
[Route("[controller]")]
public class CvController : ControllerBase
{
    private readonly ICvAnalysisService _cvAnalysisService;
    private readonly ILogger<CvController> _logger;

    /// <summary>
    /// Constructor med dependency injection
    /// Följer Dependency Inversion Principle genom att bero på interface (ICvAnalysisService)
    /// </summary>
    public CvController(ICvAnalysisService cvAnalysisService, ILogger<CvController> logger)
    {
        _cvAnalysisService = cvAnalysisService;
        _logger = logger;
    }

    /// <summary>
    /// Analyserar ett CV och extraherar kompetenser
    /// POST /cv/analyze
    /// </summary>
    /// <param name="request">CV-analysförfrågan med CV-text</param>
    /// <returns>Analysresultat med extraherade kompetenser</returns>
    /// <response code="200">CV analyserat framgångsrikt</response>
    /// <response code="400">Ogiltig förfrågan (t.ex. tom CV-text)</response>
    /// <response code="500">Internt serverfel</response>
    [HttpPost("analyze")]
    [ProducesResponseType(typeof(CvAnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AnalyzeCv([FromBody] CvAnalysisRequest request)
    {
        try
        {
            _logger.LogInformation("POST /cv/analyze anropad");

            // Validering
            if (request == null)
            {
                _logger.LogWarning("Null request mottagen");
                return BadRequest(new { error = "Request kan inte vara null" });
            }

            if (string.IsNullOrWhiteSpace(request.CvText))
            {
                _logger.LogWarning("Tom CV-text mottagen");
                return BadRequest(new { error = "CV-text kan inte vara tom" });
            }

            // Delegera all business logic till service-lagret
            var result = await _cvAnalysisService.AnalyzeCvAsync(request);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            // Hanterar valideringsfel från service-lagret
            _logger.LogWarning(ex, "Valideringsfel vid CV-analys");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            // Hanterar oväntade fel
            _logger.LogError(ex, "Oväntat fel vid CV-analys");
            return StatusCode(500, new { error = "Ett oväntat fel inträffade vid CV-analys" });
        }
    }
}

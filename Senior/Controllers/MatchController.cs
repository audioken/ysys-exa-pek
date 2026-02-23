using Microsoft.AspNetCore.Mvc;
using Senior.Models;
using Senior.Services;

namespace Senior.Controllers;

/// <summary>
/// Controller för matchning mellan CV och jobb
/// Följer Clean Architecture: Tunn controller som endast hanterar HTTP och delegerar till service-lagret
/// Följer Single Responsibility Principle - ansvarar endast för HTTP-kommunikation
/// </summary>
[ApiController]
[Route("[controller]")]
public class MatchController : ControllerBase
{
    private readonly IMatchingService _matchingService;
    private readonly ILogger<MatchController> _logger;

    /// <summary>
    /// Constructor med dependency injection
    /// Följer Dependency Inversion Principle genom att bero på interface (IMatchingService)
    /// </summary>
    public MatchController(IMatchingService matchingService, ILogger<MatchController> logger)
    {
        _matchingService = matchingService;
        _logger = logger;
    }

    /// <summary>
    /// Matchar CV mot relevanta jobb och returnerar matchningsresultat
    /// POST /match
    /// </summary>
    /// <param name="request">Matchningsförfrågan med CV-text och sökkriterier</param>
    /// <returns>Matchningsresultat med poäng och detaljer</returns>
    /// <response code="200">Matchning utförd framgångsrikt</response>
    /// <response code="400">Ogiltig förfrågan (t.ex. tom CV-text)</response>
    /// <response code="500">Internt serverfel</response>
    [HttpPost]
    [ProducesResponseType(typeof(MatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MatchCvWithJobs([FromBody] MatchRequest request)
    {
        try
        {
            _logger.LogInformation("POST /match anropad");

            // Validering
            if (request == null)
            {
                _logger.LogWarning("Null request skickad");
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Ogiltig förfrågan",
                    Detail = "Request-objektet saknas"
                });
            }

            if (string.IsNullOrWhiteSpace(request.CvText))
            {
                _logger.LogWarning("Tom CV-text skickad");
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Ogiltig förfrågan",
                    Detail = "CV-text är obligatorisk"
                });
            }

            if (request.MinimumMatchScore < 0 || request.MinimumMatchScore > 100)
            {
                _logger.LogWarning("Ogiltigt minimumpoäng: {Score}", request.MinimumMatchScore);
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Ogiltig förfrågan",
                    Detail = "MinimumMatchScore måste vara mellan 0 och 100"
                });
            }

            if (request.MaxResults < 1 || request.MaxResults > 100)
            {
                _logger.LogWarning("Ogiltigt MaxResults: {Max}", request.MaxResults);
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Ogiltig förfrågan",
                    Detail = "MaxResults måste vara mellan 1 och 100"
                });
            }

            // Delegera till service-lagret (Clean Architecture)
            var response = await _matchingService.MatchCvWithJobsAsync(request);

            _logger.LogInformation(
                "Matchning klar: {MatchCount} matchningar hittades av {TotalJobs} utvärderade jobb",
                response.Matches.Count,
                response.TotalJobsEvaluated);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid matchning av CV mot jobb");

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internt serverfel",
                Detail = "Ett oväntat fel inträffade vid matchning av CV mot jobb"
            });
        }
    }
}

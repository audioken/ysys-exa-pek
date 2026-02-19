using Microsoft.AspNetCore.Mvc;
using Senior.Services;

namespace Senior.Controllers;

/// <summary>
/// Presentation-lager: Tunn controller som delegerar allt till service-lagret
/// Följer Single Responsibility Principle - ansvarar endast för HTTP-kommunikation
/// </summary>
[ApiController]
[Route("[controller]")]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;
    private readonly ILogger<JobsController> _logger;

    public JobsController(IJobService jobService, ILogger<JobsController> logger)
    {
        _jobService = jobService;
        _logger = logger;
    }

    /// <summary>
    /// Hämtar jobb från Arbetsförmedlingens API
    /// </summary>
    /// <param name="q">Sökfråga (valfritt)</param>
    /// <param name="limit">Antal jobb att hämta (standard: 10)</param>
    /// <returns>Lista med jobb</returns>
    [HttpGet]
    public async Task<IActionResult> GetJobs([FromQuery] string? q = null, [FromQuery] int limit = 10)
    {
        try
        {
            _logger.LogInformation("GET /jobs anropad med query: {Query}, limit: {Limit}", q, limit);

            var jobs = await _jobService.GetJobsAsync(q, limit);

            return Ok(jobs);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Kunde inte hämta jobb");
            return StatusCode(503, new { error = "Tjänsten är tillfälligt otillgänglig" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Oväntat fel vid hämtning av jobb");
            return StatusCode(500, new { error = "Ett oväntat fel inträffade" });
        }
    }
}

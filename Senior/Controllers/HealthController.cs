using Microsoft.AspNetCore.Mvc;
using Senior.Services;

namespace Senior.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly IHealthService _healthService;

    public HealthController(IHealthService healthService)
    {
        _healthService = healthService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var healthStatus = _healthService.GetHealthStatus();
        return Ok(healthStatus);
    }
}

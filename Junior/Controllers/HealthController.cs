using Microsoft.AspNetCore.Mvc;
using Junior.Services;

namespace Junior.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthService _healthService;

    public HealthController(HealthService healthService)
    {
        _healthService = healthService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return _healthService.CheckHealth();
    }
}

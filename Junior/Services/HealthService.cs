using Microsoft.AspNetCore.Mvc;

namespace Junior.Services;

public class HealthService
{
    public IActionResult CheckHealth()
    {
        return new OkObjectResult(new { status = "Healthy" });
    }
}

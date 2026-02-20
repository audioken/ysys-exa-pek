using Microsoft.AspNetCore.Mvc;
using Junior.Models;
using Junior.Services;

namespace Junior.Controllers;

[ApiController]
[Route("[controller]")]
public class CvController : ControllerBase
{
    private readonly CvService _cvService;

    public CvController(CvService cvService)
    {
        _cvService = cvService;
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze([FromBody] CvAnalyzeRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.CvText))
        {
            return BadRequest(new { error = "CV text is required" });
        }

        var result = await _cvService.AnalyzeCvAsync(request.CvText);
        return Ok(result);
    }
}

using Microsoft.AspNetCore.Mvc;
using Junior.Models;
using Junior.Services;

namespace Junior.Controllers;

[ApiController]
[Route("[controller]")]
public class MatchController : ControllerBase
{
    private readonly MatchService _matchService;

    public MatchController(MatchService matchService)
    {
        _matchService = matchService;
    }

    [HttpPost]
    public async Task<IActionResult> Match([FromBody] MatchRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.CvText))
        {
            return BadRequest(new { error = "CV text is required" });
        }

        var result = await _matchService.MatchCvToJobsAsync(request.CvText);
        return Ok(result);
    }
}

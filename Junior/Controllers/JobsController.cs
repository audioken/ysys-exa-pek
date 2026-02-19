using Microsoft.AspNetCore.Mvc;
using Junior.Services;

namespace Junior.Controllers;

[ApiController]
[Route("[controller]")]
public class JobsController : ControllerBase
{
    private readonly JobsService _jobsService;

    public JobsController(JobsService jobsService)
    {
        _jobsService = jobsService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var jobs = await _jobsService.GetJobsAsync();
        return Ok(jobs);
    }
}

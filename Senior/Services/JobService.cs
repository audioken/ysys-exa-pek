using Senior.Infrastructure;
using Senior.Models;

namespace Senior.Services;

/// <summary>
/// Application-lager interface för jobbsökning
/// Definierar kontraktet för business logic
/// </summary>
public interface IJobService
{
    Task<IEnumerable<JobDto>> GetJobsAsync(string? searchQuery = null, int limit = 10);
}

/// <summary>
/// Implementation av IJobService
/// Innehåller business logic och transformerar data mellan lager
/// </summary>
public class JobService : IJobService
{
    private readonly IJobApiClient _jobApiClient;
    private readonly ILogger<JobService> _logger;

    public JobService(IJobApiClient jobApiClient, ILogger<JobService> logger)
    {
        _jobApiClient = jobApiClient;
        _logger = logger;
    }

    public async Task<IEnumerable<JobDto>> GetJobsAsync(string? searchQuery = null, int limit = 10)
    {
        _logger.LogInformation("Hämtar jobb med sökning: {Query}, limit: {Limit}", searchQuery, limit);

        try
        {
            // Anropa infrastructure-lagret
            var response = await _jobApiClient.SearchJobsAsync(searchQuery, limit);

            // Transformera från externa API-modell till intern DTO
            var jobs = response.Hits.Select(hit => new JobDto
            {
                Id = hit.Id,
                Headline = hit.Headline,
                Description = hit.Description?.Text ?? "Ingen beskrivning tillgänglig",
                Employer = hit.Employer?.Name ?? "Okänd arbetsgivare",
                Location = GetLocation(hit.Workplace_address),
                PublicationDate = hit.Publication_date,
                ApplicationDeadline = hit.Application_deadline ?? "Ingen deadline angiven"
            }).ToList();

            _logger.LogInformation("Hämtade {Count} jobb", jobs.Count);

            return jobs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av jobb");
            throw;
        }
    }

    private string GetLocation(JobWorkplace? workplace)
    {
        if (workplace == null) return "Okänd plats";

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(workplace.Municipality))
            parts.Add(workplace.Municipality);
        if (!string.IsNullOrEmpty(workplace.Region))
            parts.Add(workplace.Region);

        return parts.Any() ? string.Join(", ", parts) : "Okänd plats";
    }
}

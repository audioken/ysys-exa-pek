using System.Text.Json;

namespace Junior.Services;

public class JobsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JobsService> _logger;

    public JobsService(HttpClient httpClient, ILogger<JobsService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<object> GetJobsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("search");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch jobs. Status code: {StatusCode}", response.StatusCode);
                return new { error = "Failed to fetch jobs from API" };
            }

            var content = await response.Content.ReadAsStringAsync();
            var jobs = JsonSerializer.Deserialize<object>(content);
            
            return jobs ?? new { };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching jobs");
            return new { error = "An error occurred while fetching jobs" };
        }
    }
}

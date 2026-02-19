using System.Text.Json;
using Senior.Models;

namespace Senior.Infrastructure;

/// <summary>
/// Implementering av IJobApiClient som kommunicerar med Arbetsförmedlingens API
/// Infrastructure-lager som hanterar externa beroenden
/// </summary>
public class JobApiClient : IJobApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JobApiClient> _logger;
    public JobApiClient(HttpClient httpClient, ILogger<JobApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<JobSearchResponse> SearchJobsAsync(string? query = null, int limit = 10)
    {
        try
        {
            var queryString = $"/search?limit={limit}";
            if (!string.IsNullOrEmpty(query))
            {
                queryString += $"&q={Uri.EscapeDataString(query)}";
            }

            _logger.LogInformation("Anropar Arbetsförmedlingens API: {Url}", queryString);

            var response = await _httpClient.GetAsync(queryString);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = JsonSerializer.Deserialize<JobSearchResponse>(content, options);

            return result ?? new JobSearchResponse();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Fel vid anrop till Arbetsförmedlingens API");
            throw new InvalidOperationException("Kunde inte hämta jobb från externa API:et", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Fel vid deserialisering av API-svar");
            throw new InvalidOperationException("Fel format på data från externa API:et", ex);
        }
    }
}

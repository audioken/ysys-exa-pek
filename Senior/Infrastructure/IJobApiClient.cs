using Senior.Models;

namespace Senior.Infrastructure;

/// <summary>
/// Abstraktion för externa API-anrop
/// Gör det möjligt att mocka och testa utan att bero på det externa API:et
/// </summary>
public interface IJobApiClient
{
    Task<JobSearchResponse> SearchJobsAsync(string? query = null, int limit = 10);
}

namespace Senior.Services;

public interface IHealthService
{
    HealthResponse GetHealthStatus();
}

public class HealthService : IHealthService
{
    public HealthResponse GetHealthStatus()
    {
        // Business logic för health check
        // Kan enkelt utökas med databaskontroller, externa tjänster, etc.
        return new HealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow
        };
    }
}

public class HealthResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

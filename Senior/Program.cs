using Senior.Infrastructure;
using Senior.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Registrera services för Dependency Injection
builder.Services.AddScoped<IHealthService, HealthService>();

// Registrera Job-relaterade services (Clean Architecture)
// Application layer
builder.Services.AddScoped<IJobService, JobService>();

// Infrastructure layer - HttpClient med typed client pattern
builder.Services.AddHttpClient<IJobApiClient, JobApiClient>(client =>
{
    client.BaseAddress = new Uri("https://jobsearch.api.jobtechdev.se/");
});

var app = builder.Build();

app.MapControllers();

app.Run();

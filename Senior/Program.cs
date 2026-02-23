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

// Registrera CV-analys services (Clean Architecture)
// Application layer - Service
builder.Services.AddScoped<ICvAnalysisService, CvAnalysisService>();

// Registrera skill extractors (Strategy Pattern - gör det enkelt att lägga till nya extractors)
// Följer Open/Closed Principle - kan utöka med nya extractors utan att ändra befintlig kod
builder.Services.AddScoped<ISkillExtractor, TechnicalSkillExtractor>();
builder.Services.AddScoped<ISkillExtractor, ProgrammingLanguageExtractor>();
builder.Services.AddScoped<ISkillExtractor, FrameworkExtractor>();
builder.Services.AddScoped<ISkillExtractor, SoftSkillExtractor>();

var app = builder.Build();

app.MapControllers();

app.Run();

using Senior.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Registrera services för Dependency Injection
builder.Services.AddScoped<IHealthService, HealthService>();

var app = builder.Build();

app.MapControllers();

app.Run();

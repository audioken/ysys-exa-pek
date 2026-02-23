using Junior.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<HealthService>();
builder.Services.AddScoped<CvService>();
builder.Services.AddScoped<MatchService>();

builder.Services.AddHttpClient<JobsService>(client =>
{
    client.BaseAddress = new Uri("https://jobsearch.api.jobtechdev.se/");
});

var app = builder.Build();

app.MapControllers();

app.Run();

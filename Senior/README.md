# Senior Project - Jobs API

## Clean Architecture Implementation

Detta projekt implementerar en GET /jobs endpoint enligt Clean Architecture-principer.

## Arkitektur

### Lager

1. **Presentation Layer** (`Controllers/`)
   - `JobsController` - Tunn controller som endast hanterar HTTP-kommunikation
   - Delegerar all business logic till service-lagret

2. **Application/Service Layer** (`Services/`)
   - `IJobService` - Interface som definierar kontraktet för business logic
   - `JobService` - Implementation med business logic och data-transformering

3. **Infrastructure Layer** (`Infrastructure/`)
   - `IJobApiClient` - Abstraktion för externa API-anrop
   - `JobApiClient` - Implementation som kommunicerar med Arbetsförmedlingens API

4. **Models** (`Models/`)
   - `JobDto` - Data Transfer Object för API-responser
   - `JobSearchResponse` - Modeller för externa API-data

### Dependency Injection

All dependencies registreras i `Program.cs`:

```csharp
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddHttpClient<IJobApiClient, JobApiClient>();
```

## Användning

### Starta applikationen

```bash
dotnet run --project Senior/Senior.csproj
```

Applikationen startar på: `http://localhost:5086`

### API Endpoints

#### GET /jobs

Hämtar jobb från Arbetsförmedlingens API.

**Query Parameters:**

- `q` (optional) - Sökfråga, t.ex. "utvecklare", "lärare"
- `limit` (optional) - Antal jobb att hämta (default: 10)

**Exempel:**

```bash
# Hämta 10 jobb (standard)
curl http://localhost:5086/jobs

# Sök efter "utvecklare" och hämta 5 jobb
curl "http://localhost:5086/jobs?q=utvecklare&limit=5"

# Sök efter "lärare"
curl "http://localhost:5086/jobs?q=lärare&limit=10"
```

**Response exempel:**

```json
[
  {
    "id": "abc123",
    "headline": "Fullstack-utvecklare",
    "description": "Vi söker en erfaren utvecklare...",
    "employer": "Tech AB",
    "location": "Stockholm, Stockholms län",
    "publicationDate": "2026-02-15T10:30:00Z",
    "applicationDeadline": "2026-03-15"
  }
]
```

## Clean Architecture-fördelar

### Testbarhet

- Alla dependencies är interface-baserade
- Lätt att mocka för enhetstester
- Controller är helt isolerad från infrastruktur

### Utökbarhet

- Nya datakällor kan läggas till genom att implementera `IJobApiClient`
- Business logic kan ändras i `JobService` utan att påverka controller
- Nya endpoints kan läggas till utan att ändra befintlig kod

### Separation of Concerns

- **Controller** - Endast HTTP-hantering
- **Service** - Business logic och data-transformering
- **Infrastructure** - Externa beroenden och API-kommunikation

## Exempel på utökning

### Lägg till caching

```csharp
public class CachedJobService : IJobService
{
    private readonly IJobService _innerService;
    private readonly IMemoryCache _cache;

    // Implementation med caching
}
```

### Lägg till en annan datakälla

```csharp
public class DatabaseJobApiClient : IJobApiClient
{
    private readonly DbContext _context;

    // Implementation som hämtar från databas
}
```

### Enhetstester

```csharp
[Fact]
public async Task GetJobs_ReturnsJobs()
{
    // Arrange
    var mockApiClient = new Mock<IJobApiClient>();
    var service = new JobService(mockApiClient.Object, logger);

    // Act
    var result = await service.GetJobsAsync();

    // Assert
    Assert.NotEmpty(result);
}
```

## Beroenden

- ASP.NET Core 9.0
- System.Text.Json (inbyggt)
- HttpClient (inbyggt)

## Externa API

Användare: Arbetsförmedlingens Job Search API

- Bas URL: https://jobsearch.api.jobtechdev.se
- Dokumentation: https://jobtechdev.se/docs/apis/jobsearch/

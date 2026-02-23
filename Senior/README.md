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

#### POST /match

Matchar CV-text mot relevanta jobb och returnerar matchningsresultat med poäng.

**Request Body:**

```json
{
  "cvText": "string (obligatorisk) - CV-text som ska matchas",
  "jobIds": ["string"] (optional) - Lista med specifika jobb-ID:n att matcha mot",
  "searchQuery": "string (optional) - Sökord för att hitta relevanta jobb",
  "location": "string (optional) - Platsfilter för jobb",
  "radiusKm": "number (optional) - Geografisk radie i km",
  "minimumMatchScore": "number (default: 0) - Minsta matchningspoäng 0-100",
  "maxResults": "number (default: 10) - Max antal matchningar att returnera"
}
```

**Response:**

```json
{
  "matches": [
    {
      "job": {
        /* JobDto */
      },
      "matchScore": 85,
      "matchedSkills": ["C#", ".NET", "Azure", "SQL"],
      "missingSkills": ["React", "TypeScript"],
      "matchExplanation": "Matchning: 4 av 6 kompetenser (67%). Starka matchningar: C# (3x), .NET (2x)."
    }
  ],
  "totalJobsEvaluated": 50,
  "extractedSkills": ["C#", ".NET", "Azure", "SQL", "Docker", "Kubernetes"],
  "matchingStrategy": "Skill-Based Matching"
}
```

**Exempel:**

```bash
# Matcha CV mot jobb
curl -X POST http://localhost:5086/match \
  -H "Content-Type: application/json" \
  -d '{
    "cvText": "Erfaren .NET-utvecklare med 5 års erfarenhet av C#, ASP.NET Core och Azure.",
    "searchQuery": "developer",
    "maxResults": 5,
    "minimumMatchScore": 50
  }'
```

**PowerShell-testskript:**

```bash
.\test-match-api.ps1
```

#### GET /health

Hälsokontroll för applikationen.

**Response exempel:**

```json
{
  "status": "Healthy",
  "timestamp": "2026-02-23T10:30:00Z"
}
```

#### POST /cv/analyze

Analyserar CV-text och extraherar kompetenser.

**Request Body:**

```json
{
  "cvText": "string (obligatorisk) - CV-text som ska analyseras",
  "targetRole": "string (optional) - Målyrke för mer specifik analys"
}
```

**Response exempel:**

```json
{
  "technicalSkills": ["Clean Architecture", "SOLID", "Design Patterns"],
  "softSkills": ["Teamwork", "Communication"],
  "programmingLanguages": ["C#", "Python", "JavaScript"],
  "frameworks": ["ASP.NET Core", "React"],
  "yearsOfExperience": 5,
  "summary": "Erfaren utvecklare med bred teknisk kompetens..."
}
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
- Nya matchningsstrategier kan läggas till genom att implementera `IMatchingStrategy`

### Separation of Concerns

- **Controller** - Endast HTTP-hantering
- **Service** - Business logic och data-transformering
- **Infrastructure** - Externa beroenden och API-kommunikation
- **Strategy** - Utbytbara algoritmer för matchning och kompetensextraktion

## SOLID-principer i POST /match

### Single Responsibility Principle (SRP)

- `MatchController` - Ansvarar endast för HTTP-kommunikation
- `MatchingService` - Ansvarar för att koordinera matchningsprocessen
- `IMatchingStrategy` - Ansvarar för själva matchningsalgoritmen

### Open/Closed Principle (OCP)

- Nya matchningsstrategier kan läggas till utan att ändra `MatchingService`
- Implementera `IMatchingStrategy` och registrera i DI-container
- Exempel: `SkillBasedMatchingStrategy`, `KeywordMatchingStrategy`

### Liskov Substitution Principle (LSP)

- Alla `IMatchingStrategy`-implementationer är utbytbara
- Alla `ISkillExtractor`-implementationer är utbytbara
- Byt strategi genom att ändra DI-registrering i `Program.cs`

### Interface Segregation Principle (ISP)

- `IMatchingService` - Minimalt interface med endast `MatchCvWithJobsAsync`
- `IMatchingStrategy` - Fokuserat interface för matchningsalgoritmer
- Inga onödiga dependencies för implementeringar

### Dependency Inversion Principle (DIP)

- `MatchController` beror på `IMatchingService` (inte konkret klass)
- `MatchingService` beror på `IMatchingStrategy`, `ICvAnalysisService`, `IJobService`
- Alla beroenden injiceras via constructor
- Registrering i `Program.cs` gör det enkelt att byta implementationer

### Strategy Pattern

Matchningssystemet använder Strategy Pattern för att göra matchningsalgoritmer utbytbara:

```csharp
// I Program.cs kan du välja strategi:
builder.Services.AddScoped<IMatchingStrategy, SkillBasedMatchingStrategy>();
// Eller:
// builder.Services.AddScoped<IMatchingStrategy, KeywordMatchingStrategy>();

// Lägg till egen strategi:
public class CustomMatchingStrategy : IMatchingStrategy
{
    public string StrategyName => "Custom Matching";

    public Task<MatchResult> CalculateMatchAsync(List<string> cvSkills, JobDto job)
    {
        // Din matchningslogik här
    }
}
```

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

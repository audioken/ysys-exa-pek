# Jämförelse och analys av Jobs Endpoint - tre utvecklarnivåer

## Översikt av implementationerna

### **Novis-nivå**
- En controller med direkta HTTP-anrop till externt API
- Använder `IHttpClientFactory` för att skapa HTTP-klienter
- Felhantering direkt i controllern med try-catch
- Returnerar rå JSON-sträng utan deserialisering
- Ingen service-logik eller models
- Total: ~35 rader kod i 1 fil

### **Junior-nivå**
- Controller som delegerar till en service
- Konkret `JobsService`-klass injicerad
- Service returnerar `object` (odefiniert typ)
- Felhantering i servicen som returnerar error-objekt
- Använder `HttpClient` via dependency injection
- Inga starka typer eller DTOs
- Total: ~60 rader kod i 2 filer

### **Senior-nivå**
- Controller som delegerar via `IJobService` interface
- Service-lager (`JobService`) för affärslogik
- Dedikerat infrastructure-lager (`IJobApiClient`) för externa anrop
- Tydliga modeller: `JobDto` (output), `JobSearchResponse` (input)
- Separation mellan external API models och interna DTOs
- Datatransformation mellan lager
- Query parameters för flexibel sökning
- Logging and felhantering på varje nivå
- Total: ~230 rader kod i 6 filer (controller, service, infrastructure, 3 modeller)

---

## Analys utifrån SOLID-principer

| **Princip** | **Novis** | **Junior** | **Senior** |
|---|---|---|---|
| **SRP** | ❌ Controller gör allt - HTTP-anrop, felhantering, serialisering | ⚠️ Delvis - men service returnerar `object` och hanterar HTTP-fel | ✅ Tydlig ansvarsfördelning över 3 lager |
| **OCP** | ❌ Omöjligt att utöka utan att ändra controllern | ⚠️ Kan utökas men kräver ändringar i service | ✅ Kan utökas med nya implementationer via interfaces |
| **LSP** | N/A | N/A | ✅ Both `IJobService` och `IJobApiClient` substituerbar |
| **ISP** | N/A | ⚠️ Ingen interface-segregation | ✅ Fokuserade interfaces för varje ansvar |
| **DIP** | ⚠️ Beror på `IHttpClientFactory` (bra!) men skapar HttpClient själv | ⚠️ Beror på konkret `JobsService` | ✅ Beror på abstraktioner på alla nivåer |

---

## Analys utifrån Arkitekturprinciper

### **Separation of Concerns (SoC)**

**Novis:** Allvarlig brist på separation - controllern hanterar:
- HTTP-kommunikation till externt API
- Felhantering
- Status code-kontroll
- Content-deserialisering (eller snarare, returnering av rå JSON)

```csharp
var client = _httpClientFactory.CreateClient();
var response = await client.GetAsync("https://jobsearch.api.jobtechdev.se/search");
// ❌ Controller gör externa API-anrop direkt
```

**Junior:** Delvis separation - controllern delegerar till service, MEN:
- Service returnerar `object` vilket är otydligt och otypsäkert
- Service hanterar både HTTP-anrop OCH felmeddelanden som domänobjekt
- Blandar infrastructure-concerns (HTTP) med business logic

```csharp
public async Task<object> GetJobsAsync()  // ❌ 'object' är inte en tydlig kontrakt
{
    // ...
    return new { error = "Failed to fetch jobs from API" };  // ❌ Service returnerar error-objekt
}
```

**Senior:** Exemplarisk separation över tre lager:

1. **Presentation (Controller)** - HTTP-hantering, query params, status codes
2. **Application (Service)** - Affärslogik, datatransformation
3. **Infrastructure (ApiClient)** - Externa API-anrop, serialisering

```csharp
// Controller
var jobs = await _jobService.GetJobsAsync(q, limit);
return Ok(jobs);

// Service
var response = await _jobApiClient.SearchJobsAsync(searchQuery, limit);
var jobs = response.Hits.Select(hit => new JobDto { ... });

// Infrastructure
var response = await _httpClient.GetAsync(queryString);
return JsonSerializer.Deserialize<JobSearchResponse>(content);
```

Varje lager har sitt tydliga ansvar och returnerar starkt typade objekt.

### **Layered Architecture / Clean Architecture**

**Novis:** Ingen lagerindelning - allt i presentation-lagret. Violerar fundamentala arkitekturprinciper genom att blanda HTTP med presentation utan abstraktion.

**Junior:** Två lager (Controller, Service) men med otydliga gränser:
- Service gör infrastructure-arbete (HTTP-anrop)
- Service returnerar otydlig typ (`object`)
- Ingen transformation mellan externa och interna modeller

**Senior:** Klassisk 3-lager arkitektur med tydliga beroenden:

```
┌──────────────────────────────────────┐
│   Presentation Layer                 │
│   (JobsController)                   │
│   - HTTP request/response            │
│   - Query parameters                 │
│   - Status codes                     │
└──────────┬───────────────────────────┘
           │ depends on IJobService
           ▼
┌──────────────────────────────────────┐
│   Application Layer                  │
│   (IJobService / JobService)         │
│   - Business logic                   │
│   - Data transformation              │
│   - Returns JobDto                   │
└──────────┬───────────────────────────┘
           │ depends on IJobApiClient
           ▼
┌──────────────────────────────────────┐
│   Infrastructure Layer               │
│   (IJobApiClient / JobApiClient)     │
│   - External API calls               │
│   - HTTP communication               │
│   - Returns JobSearchResponse        │
└──────────────────────────────────────┘
```

Beroenden pekar inåt - varje lager beror på abstraktioner, inte konkreta implementationer.

### **Dependency Injection**

**Novis:** ⚠️ Delvis DI - använder `IHttpClientFactory` (vilket är best practice!) men skapar HttpClient i controllern:

```csharp
var client = _httpClientFactory.CreateClient();  // ✅ Använder factory
var response = await client.GetAsync(...);       // ❌ HTTP-logik i controller
```

Registrering i `Program.cs`:
```csharp
builder.Services.AddHttpClient();
```

**Junior:** ⚠️ Constructor injection av konkret klass och HttpClient:

```csharp
public JobsController(JobsService jobsService)  // ❌ Konkret klass
```

Registrering i `Program.cs`:
```csharp
builder.Services.AddHttpClient<JobsService>(client =>
{
    client.BaseAddress = new Uri("https://jobsearch.api.jobtechdev.se");
});
```

Bra användning av typed HttpClient, men binder till konkret implementation.

**Senior:** ✅ Fullständig DI på alla nivåer:

```csharp
public JobsController(IJobService jobService, ILogger<JobsController> logger)
public JobService(IJobApiClient jobApiClient, ILogger<JobService> logger)
public JobApiClient(HttpClient httpClient, ILogger<JobApiClient> logger)
```

Registrering i `Program.cs`:
```csharp
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddHttpClient<IJobApiClient, JobApiClient>();
```

Möjliggör enkel byte av implementation eller mockning för tester på varje nivå.

### **Data Transformation och Type Safety**

**Novis:** ❌ Ingen transformation - returnerar rå JSON som sträng:

```csharp
var content = await response.Content.ReadAsStringAsync();
return Content(content, "application/json");  // ❌ Rå sträng, ingen kontroll
```

Problem:
- Ingen validering av data
- Klienten får externa API:ets format direkt
- Ändringar i externa API:et påverkar direkt klienter
- Omöjligt att testa utan faktiska API-anrop

**Junior:** ⚠️ Deserialiserar men till `object`:

```csharp
var jobs = JsonSerializer.Deserialize<object>(content);  // ❌ Ingen typ-säkerhet
return jobs ?? new { };
```

Problem:
- Ingen typ-säkerhet
- Ingen IntelliSense för konsumenter
- Runtime-fel istället för compile-time
- Otydligt kontrakt mellan service och controller

**Senior:** ✅ Fullständig typ-säkerhet med transformation:

```csharp
// External API model
public class JobSearchResponse
{
    public List<JobSearchHit> Hits { get; set; }
}

// Internal DTO
public class JobDto
{
    public string Id { get; set; }
    public string Headline { get; set; }
    public string Description { get; set; }
    // ...
}

// Transformation i service-lagret
var jobs = response.Hits.Select(hit => new JobDto
{
    Id = hit.Id,
    Headline = hit.Headline,
    Description = hit.Description?.Text ?? "Ingen beskrivning tillgänglig",
    Employer = hit.Employer?.Name ?? "Okänd arbetsgivare",
    Location = GetLocation(hit.Workplace_address),
    // ...
}).ToList();
```

Fördelar:
- Externa API-ändringar isolerade till infrastructure-lagret
- Klienten får konsistent format
- Typ-säkerhet genom hela stacken
- Möjlighet att filtrera/transformera data
- Enkel att testa med mock-data

---

## Felhantering

### **Novis:** Basic try-catch i controller

```csharp
try
{
    var response = await client.GetAsync(...);
    if (response.IsSuccessStatusCode)
        return Content(content, "application/json");
    return StatusCode((int)response.StatusCode, "Failed to fetch jobs");
}
catch (Exception ex)
{
    return StatusCode(500, $"Error: {ex.Message}");  // ❌ Exponerar exception-meddelande
}
```

Problem:
- Exponerar tekniska detaljer till klient
- Ingen logging
- Generisk felhantering
- Svårt att skilja mellan olika feltyper

### **Junior:** Felhantering i service, men returnerar error som data

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Error occurred while fetching jobs");
    return new { error = "An error occurred while fetching jobs" };  // ❌ Error som data
}
```

Problem:
- Blandas datastrukturer (success och error har olika format)
- Klienten måste kontrollera om resultatet är error
- Service returnerar `object` gör att detta inte är typkontrollerat
- Ingen distinktion mellan olika HTTP-statuskoder

### **Senior:** Strukturerad felhantering per lager

**Infrastructure-lager** - Kastar specifika exceptions:
```csharp
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "Fel vid anrop till Arbetsförmedlingens API");
    throw new InvalidOperationException("Kunde inte hämta jobb från externa API:et", ex);
}
```

**Application-lager** - Loggar och Re-throw:
```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Fel vid hämtning av jobb");
    throw;  // ✅ Låter controller hantera HTTP-respons
}
```

**Presentation-lager** - Översätter till HTTP-statuskoder:
```csharp
catch (InvalidOperationException ex)
{
    _logger.LogWarning(ex, "Kunde inte hämta jobb");
    return StatusCode(503, new { error = "Tjänsten är tillfälligt otillgänglig" });
}
catch (Exception ex)
{
    _logger.LogError(ex, "Oväntat fel vid hämtning av jobb");
    return StatusCode(500, new { error = "Ett oväntat fel inträffade" });
}
```

Fördelar:
- Tydlig separation: Infrastructure kastar, Service propagerar, Controller svarar
- Specifika exceptions för olika feltyper
- Logging på varje nivå
- Klienten får lämpliga HTTP-statuskoder
- Inga interna detaljer exponeras

---

## Testbarhet

| **Aspekt** | **Novis** | **Junior** | **Senior** |
|---|---|---|---|
| Unit-testning av controller | ❌ Måste mocka IHttpClientFactory OCH göra HTTP-anrop | ⚠️ Måste mocka konkret JobsService | ✅ Enkelt - mocka IJobService |
| Unit-testning av service | N/A | ⚠️ Svårt - service gör HTTP-anrop direkt | ✅ Enkelt - mocka IJobApiClient |
| Unit-testning av infrastructure | N/A | N/A | ✅ Enkelt - mocka HttpClient |
| Integration-testning | ❌ Svårt - allt i en klass | ⚠️ Måste mocka både service OCH HttpClient | ✅ Tydliga gränser per lager |

### **Testexempel - Novis (Svårt)**

```csharp
// Måste mocka IHttpClientFactory och HttpMessageHandler
var mockFactory = new Mock<IHttpClientFactory>();
var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
mockHttpMessageHandler
    .Protected()
    .Setup<Task<HttpResponseMessage>>("SendAsync", ...)
    .ReturnsAsync(new HttpResponseMessage { ... });

var client = new HttpClient(mockHttpMessageHandler.Object);
mockFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

var controller = new JobsController(mockFactory.Object);
// ❌ Komplex setup för enkel test
```

### **Testexempel - Junior (Medel)**

```csharp
// Måste mocka konkret klass (kräver virtual methods eller interface)
var mockService = new Mock<JobsService>();  // ❌ Fungerar inte utan virtual methods
mockService.Setup(s => s.GetJobsAsync()).ReturnsAsync(new { ... });

var controller = new JobsController(mockService.Object);
```

Problem: Eftersom vi mockar konkret klass utan interface, fungerar detta inte direkt.

### **Testexempel - Senior (Enkelt)**

**Controller test:**
```csharp
var mockService = new Mock<IJobService>();
mockService.Setup(s => s.GetJobsAsync(null, 10))
    .ReturnsAsync(new List<JobDto>
    {
        new JobDto { Id = "1", Headline = "C# Developer" }
    });

var mockLogger = new Mock<ILogger<JobsController>>();
var controller = new JobsController(mockService.Object, mockLogger.Object);

var result = await controller.GetJobs();
// ✅ Enkelt, tydligt, isolerat
```

**Service test:**
```csharp
var mockApiClient = new Mock<IJobApiClient>();
mockApiClient.Setup(c => c.SearchJobsAsync(null, 10))
    .ReturnsAsync(new JobSearchResponse
    {
        Hits = new List<JobSearchHit>
        {
            new JobSearchHit { Id = "1", Headline = "Developer" }
        }
    });

var service = new JobService(mockApiClient.Object, mockLogger.Object);

var result = await service.GetJobsAsync(null, 10);
// ✅ Testar transformation-logik isolerat
```

**Infrastructure test:**
```csharp
var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
mockHttpMessageHandler
    .Protected()
    .Setup<Task<HttpResponseMessage>>("SendAsync", ...)
    .ReturnsAsync(new HttpResponseMessage
    {
        Content = new StringContent(mockJsonResponse)
    });

var httpClient = new HttpClient(mockHttpMessageHandler.Object);
var apiClient = new JobApiClient(httpClient, mockLogger.Object);

var result = await apiClient.SearchJobsAsync("Developer", 10);
// ✅ Testar HTTP-kommunikation isolerat
```

---

## Underhållbarhet och utbyggbarhet

### **Scenario 1: Lägg till caching av jobbsökningar**

**Novis:** 
- Måste ändra controllern direkt ❌
- Lägga till cache-logik mitt i HTTP-kod ❌
- Bryter OCP helt ❌

**Junior:**
- Kan lägga till i `JobsService` ⚠️
- Men service gör redan HTTP-anrop, så cachen blandas med HTTP-logik ⚠️
- Svårt att testa cache-logik separat ⚠️

**Senior:**
- Skapa `CachedJobService` som implementerar `IJobService` ✅
- Använder decorator-pattern ✅
- Ingen ändring i controller eller infrastructure ✅

```csharp
public class CachedJobService : IJobService
{
    private readonly IJobService _innerService;
    private readonly IMemoryCache _cache;

    public CachedJobService(IJobService innerService, IMemoryCache cache)
    {
        _innerService = innerService;
        _cache = cache;
    }

    public async Task<IEnumerable<JobDto>> GetJobsAsync(string? searchQuery, int limit)
    {
        var cacheKey = $"jobs_{searchQuery}_{limit}";
        
        if (_cache.TryGetValue(cacheKey, out IEnumerable<JobDto>? cachedJobs))
            return cachedJobs!;

        var jobs = await _innerService.GetJobsAsync(searchQuery, limit);
        _cache.Set(cacheKey, jobs, TimeSpan.FromMinutes(5));
        
        return jobs;
    }
}

// I Program.cs - ändra registrering
builder.Services.AddScoped<JobService>();
builder.Services.AddScoped<IJobService, CachedJobService>();
```

OCP respekteras - ingen befintlig kod ändras.

### **Scenario 2: Byt till ett annat jobb-API**

**Novis:**
- Måste skriva om hela controllern ❌
- Ändringar påverkar HTTP-lager direkt ❌

**Junior:**
- Måste ändra `JobsService` ⚠️
- Om API-formatet ändras, påverkas controller också (eftersom service returnerar `object`) ⚠️

**Senior:**
- Skapa ny `AlternativeJobApiClient` som implementerar `IJobApiClient` ✅
- Ingen ändring i service eller controller ✅
- Datatransformation i service-lagret skyddar övre lager från ändringar ✅

```csharp
public class AlternativeJobApiClient : IJobApiClient
{
    public async Task<JobSearchResponse> SearchJobsAsync(string? query, int limit)
    {
        // Anropa nytt API
        var externalData = await _httpClient.GetAsync("new-api-endpoint");
        
        // Transformera till JobSearchResponse (samma format)
        return TransformToJobSearchResponse(externalData);
    }
}

// I Program.cs
builder.Services.AddHttpClient<IJobApiClient, AlternativeJobApiClient>();
```

### **Scenario 3: Lägg till filtreringslogik**

**Novis:** Måste lägga till i controller ❌

**Junior:** Kan lägga till i service, men sker före eller efter HTTP-anrop? ⚠️

**Senior:** Lägg till i `JobService.GetJobsAsync()` ✅

```csharp
public async Task<IEnumerable<JobDto>> GetJobsAsync(string? searchQuery, int limit)
{
    var response = await _jobApiClient.SearchJobsAsync(searchQuery, limit);
    
    var jobs = response.Hits
        .Select(hit => new JobDto { ... })
        .Where(job => !string.IsNullOrEmpty(job.Employer))  // ✅ Lägg till filter här
        .Take(limit)
        .ToList();
    
    return jobs;
}
```

Affärslogik i rätt lager, ingen påverkan på andra delar.

---

## Kodens läsbarhet och Intent

### **Novis:**
```csharp
var client = _httpClientFactory.CreateClient();
var response = await client.GetAsync("https://jobsearch.api.jobtechdev.se/search");
```

Problem:
- Otydligt vad som händer
- Hårdkodad URL i controller
- Blandar HTTP-kommunikation med presentation

### **Junior:**
```csharp
var jobs = await _jobsService.GetJobsAsync();
return Ok(jobs);
```

Bättre, men:
- `jobs` är av typ `object` - vad är det egentligen?
- `GetJobsAsync()` avslöjar inte vad servicen gör

### **Senior:**
```csharp
var jobs = await _jobService.GetJobsAsync(q, limit);
return Ok(jobs);
```

Tydlig intent:
- `jobs` är `IEnumerable<JobDto>` - tydligt vad som returneras
- Query parameters visar att endpoint är sökbar
- Självdokumenterande kod med XML-kommentarer

---

## Uppfyllnad av examensarbetets kriterier

### **Novis - "Funktion över struktur"**

✅ **Uppfyller förväntningarna:**
- Regelsstyrt: Använder IHttpClientFactory (best practice!)
- Begränsad situationsförståelse: Ingen separation mellan lager
- Fokus på funktion: Hämtar och returnerar jobb som efterfrågats
- Ingen långsiktig kvalitet: Omöjligt att underhålla vid tillväxt

**Bedömning:** Kodkvalitet motsvarar "quick and dirty script" - fungerar men inte produktionsklar.

### **Junior - "Struktur men utan djup arkitektonisk kontroll"**

⚠️ **Uppfyller delvis, med betydande brister:**
- Situationsförståelse: Förstår att använda service-lager ✅
- Tillämpar riktlinjer: Försöker separera concerns ✅
- Men: Service returnerar `object` visar allvarlig brist på typ-säkerhet ❌
- Men: Service gör HTTP-anrop direkt (infrastructure i application layer) ❌
- Men: Ingen interface-användning ❌
- Men: Returnerar error som data istället för exceptions ❌

**Bedömning:** Kodkvalitet motsvarar en utvecklare som "förstår grunderna men saknar erfarenhet av enterprise-arkitektur".

### **Senior - "Arkitektur, underhållbarhet och kodkvalitet"**

✅ **Uppfyller förväntningarna helt:**
- Helhetsförståelse: Tydlig 3-lager arkitektur
- SOLID-principer: Alla tillämpade korrekt
- Clean Architecture: Infrastructure beror på abstraktion, inte tvärtom
- Separation of Concerns: Ren separation mellan presentation, application, infrastructure
- Data Transformation: Externa modeller transformeras till interna DTOs
- Type Safety: Starkt typade kontrakt genom hela stacken
- Testbarhet: Fullständigt mockbar på varje nivå
- Logging: Strukturerad logging per lager
- Felhantering: Exception-baserad med tydlig ansvarsfördelning

**Bedömning:** Kodkvalitet motsvarar professionell enterprise-standard med Clean Architecture principer.

---

## Sammanfattande kvalitetsbedömning

| **Kriterium** | **Novis** | **Junior** | **Senior** |
|---|---|---|---|
| Funktionalitet | ✅ Fungerar | ✅ Fungerar | ✅ Fungerar |
| SOLID-principer | 1/5 | 2/5 | 5/5 |
| Arkitektur (Lager) | 0/5 | 2/5 | 5/5 |
| Testbarhet | 1/5 | 2/5 | 5/5 |
| Underhållbarhet | 1/5 | 2/5 | 5/5 |
| Utbyggbarhet | 1/5 | 2/5 | 5/5 |
| Typ-säkerhet | 1/5 | 1/5 | 5/5 |
| Felhantering | 2/5 | 2/5 | 5/5 |
| **Totalt** | **7/40** | **13/40** | **40/40** |

---

## Kritiska observationer för examensarbetet

### **1. Komplexitet avslöjar kompetens mer än enkla endpoints**

Health endpoint-analysen visade skillnader, men **Jobs endpoint-analysen visar DRAMATISKA skillnader**:

- Health: Senior hade 7 extra rader vs Junior
- Jobs: Senior har **~170 extra rader** vs Junior

Men dessa extra rader representerar:
- Tydlig arkitektur med 3 lager
- Typ-säkerhet genom hela stacken
- Testbarhet utan komplexa mockar
- Möjlighet att byta implementation utan att ändra övre lager

### **2. `object` som returtyp är en allvarlig anti-pattern**

Junior-implementationen returnerar `object` från service:

```csharp
public async Task<object> GetJobsAsync()
```

Detta är **extremt problematiskt**:
- ❌ Förlorar all typ-information vid kompilering
- ❌ Inga IntelliSense-fördelar
- ❌ Runtime-fel istället för compile-time
- ❌ Omöjligt att veta vad som returneras utan att läsa implementation
- ❌ Blandar success och error i samma returtyp

Detta visar fundamental brist på förståelse för typ-system och kontrakt.

### **3. Infrastructure i Application Layer är arkitekturfel**

Junior-implementationen har HTTP-anrop i service-lagret:

```csharp
public class JobsService  // Application layer
{
    public async Task<object> GetJobsAsync()
    {
        var response = await _httpClient.GetAsync("search");  // ❌ Infrastructure concern
    }
}
```

Detta bryter mot **Clean Architecture** där applikationslagret inte ska bero på externa detaljer som HTTP.

Senior-implementationen separerar korrekt:
- `JobService` (Application) - affärslogik, transformation
- `JobApiClient` (Infrastructure) - HTTP-kommunikation

### **4. Transformation mellan lager möjliggör flexibilitet**

Senior har **medveten datatransformation**:

```
External API (JobSearchResponse) 
    → Infrastructure Layer deserialiserar
    → Application Layer transformerar
    → Internal DTO (JobDto)
    → Presentation Layer returnerar
```

Detta skyddar systemet från externa ändringar och möjliggör:
- Filtrering av känslig data
- Anpassning av fältnamn
- Aggregering av data
- Versionshantering av API

Junior har ingen transformation - returnerar rå data som `object`.

### **5. Mängd kod vs kvalitet på abstraktioner**

| Metric | Novis | Junior | Senior |
|---|---|---|---|
| Antal filer | 1 | 2 | 6 |
| Rader kod | ~35 | ~60 | ~230 |
| Antal interfaces | 0 | 0 | 2 |
| Antal models | 0 | 0 | 3 |
| Testbarhet | Minimal | Svår | Trivial |
| Change impact | Hög | Medel | Låg |

Senior-koden är **~4x mer kod**, men **~10x enklare att underhålla och testa**.

### **6. Novis använder faktiskt en best practice**

Novis använder `IHttpClientFactory`:

```csharp
var client = _httpClientFactory.CreateClient();
```

Detta är **korrekt** enligt Microsoft best practices för HttpClient-hantering! Det förhindrar socket exhaustion.

Junior och Senior använder istället typed HttpClient via dependency injection, vilket är en högre abstraktion men bygger på samma factory-mönster.

Detta visar att **även "novice" code kan innehålla best practices om prompten nämner dem**.

---

## Visualisering av arkitekturen

### **Novis - Flat Architecture**
```
┌────────────────────────────┐
│   JobsController           │
│   - HTTP requests          │
│   - API calls              │
│   - Error handling         │
│   - JSON return            │
└────────────────────────────┘
```

### **Junior - Pseudo-Layered**
```
┌────────────────────────────┐
│   JobsController           │
│   - HTTP request           │
└────────────┬───────────────┘
             │ (concrete class)
             ▼
┌────────────────────────────┐
│   JobsService              │
│   - HTTP calls             │  ← ❌ Infrastructure in service
│   - Error handling         │
│   - Returns object         │  ← ❌ No type safety
└────────────────────────────┘
```

### **Senior - Clean Architecture**
```
┌─────────────────────────────────────┐
│   JobsController                    │
│   - HTTP request/response           │
│   - Query parameters                │
│   - Status codes                    │
└───────────┬─────────────────────────┘
            │ IJobService
            ▼
┌─────────────────────────────────────┐
│   JobService                        │
│   - Business logic                  │
│   - Data transformation             │
│   - JobSearchResponse → JobDto      │
└───────────┬─────────────────────────┘
            │ IJobApiClient
            ▼
┌─────────────────────────────────────┐
│   JobApiClient                      │
│   - HTTP communication              │
│   - External API calls              │
│   - Deserialization                 │
└─────────────────────────────────────┘
```

---

## Kodexempel från implementationerna

### Novis-nivå

```csharp
// Novis/Controllers/JobsController.cs
using Microsoft.AspNetCore.Mvc;

namespace Novis.Controllers;

[ApiController]
[Route("[controller]")]
public class JobsController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public JobsController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync("https://jobsearch.api.jobtechdev.se/search");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
            
            return StatusCode((int)response.StatusCode, "Failed to fetch jobs");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }
}
```

### Junior-nivå

```csharp
// Junior/Controllers/JobsController.cs
using Microsoft.AspNetCore.Mvc;
using Junior.Services;

namespace Junior.Controllers;

[ApiController]
[Route("[controller]")]
public class JobsController : ControllerBase
{
    private readonly JobsService _jobsService;

    public JobsController(JobsService jobsService)
    {
        _jobsService = jobsService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var jobs = await _jobsService.GetJobsAsync();
        return Ok(jobs);
    }
}
```

```csharp
// Junior/Services/JobsService.cs
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
```

### Senior-nivå

```csharp
// Senior/Controllers/JobsController.cs
using Microsoft.AspNetCore.Mvc;
using Senior.Services;

namespace Senior.Controllers;

[ApiController]
[Route("[controller]")]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;
    private readonly ILogger<JobsController> _logger;

    public JobsController(IJobService jobService, ILogger<JobsController> logger)
    {
        _jobService = jobService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetJobs([FromQuery] string? q = null, [FromQuery] int limit = 10)
    {
        try
        {
            _logger.LogInformation("GET /jobs anropad med query: {Query}, limit: {Limit}", q, limit);

            var jobs = await _jobService.GetJobsAsync(q, limit);

            return Ok(jobs);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Kunde inte hämta jobb");
            return StatusCode(503, new { error = "Tjänsten är tillfälligt otillgänglig" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Oväntat fel vid hämtning av jobb");
            return StatusCode(500, new { error = "Ett oväntat fel inträffade" });
        }
    }
}
```

```csharp
// Senior/Services/JobService.cs
using Senior.Infrastructure;
using Senior.Models;

namespace Senior.Services;

public interface IJobService
{
    Task<IEnumerable<JobDto>> GetJobsAsync(string? searchQuery = null, int limit = 10);
}

public class JobService : IJobService
{
    private readonly IJobApiClient _jobApiClient;
    private readonly ILogger<JobService> _logger;

    public JobService(IJobApiClient jobApiClient, ILogger<JobService> logger)
    {
        _jobApiClient = jobApiClient;
        _logger = logger;
    }

    public async Task<IEnumerable<JobDto>> GetJobsAsync(string? searchQuery = null, int limit = 10)
    {
        _logger.LogInformation("Hämtar jobb med sökning: {Query}, limit: {Limit}", searchQuery, limit);

        try
        {
            var response = await _jobApiClient.SearchJobsAsync(searchQuery, limit);

            var jobs = response.Hits.Select(hit => new JobDto
            {
                Id = hit.Id,
                Headline = hit.Headline,
                Description = hit.Description?.Text ?? "Ingen beskrivning tillgänglig",
                Employer = hit.Employer?.Name ?? "Okänd arbetsgivare",
                Location = GetLocation(hit.Workplace_address),
                PublicationDate = hit.Publication_date,
                ApplicationDeadline = hit.Application_deadline ?? "Ingen deadline angiven"
            }).ToList();

            _logger.LogInformation("Hämtade {Count} jobb", jobs.Count);

            return jobs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid hämtning av jobb");
            throw;
        }
    }

    private string GetLocation(JobWorkplace? workplace)
    {
        if (workplace == null) return "Okänd plats";

        var parts = new List<string>();
        if (!string.IsNullOrEmpty(workplace.Municipality))
            parts.Add(workplace.Municipality);
        if (!string.IsNullOrEmpty(workplace.Region))
            parts.Add(workplace.Region);

        return parts.Any() ? string.Join(", ", parts) : "Okänd plats";
    }
}
```

```csharp
// Senior/Infrastructure/IJobApiClient.cs
using Senior.Models;

namespace Senior.Infrastructure;

public interface IJobApiClient
{
    Task<JobSearchResponse> SearchJobsAsync(string? query = null, int limit = 10);
}
```

```csharp
// Senior/Infrastructure/JobApiClient.cs
using System.Text.Json;
using Senior.Models;

namespace Senior.Infrastructure;

public class JobApiClient : IJobApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JobApiClient> _logger;
    private const string BaseUrl = "https://jobsearch.api.jobtechdev.se";

    public JobApiClient(HttpClient httpClient, ILogger<JobApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(BaseUrl);
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
```

```csharp
// Senior/Models/JobDto.cs
namespace Senior.Models;

public class JobDto
{
    public string Id { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Employer { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime PublicationDate { get; set; }
    public string ApplicationDeadline { get; set; } = string.Empty;
}
```

```csharp
// Senior/Models/JobSearchResponse.cs
namespace Senior.Models;

public class JobSearchResponse
{
    public int Total { get; set; }
    public List<JobSearchHit> Hits { get; set; } = new();
}

public class JobSearchHit
{
    public string Id { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public JobDescription? Description { get; set; }
    public JobEmployer? Employer { get; set; }
    public JobWorkplace? Workplace_address { get; set; }
    public DateTime Publication_date { get; set; }
    public string? Application_deadline { get; set; }
}

public class JobDescription
{
    public string? Text { get; set; }
}

public class JobEmployer
{
    public string? Name { get; set; }
}

public class JobWorkplace
{
    public string? Municipality { get; set; }
    public string? Region { get; set; }
}
```

---

## Rekommendationer för examensarbetet

### För resultatredovisningen:

1. **Använd Jobs endpoint som huvudexempel** - Den visar mycket tydligare skillnader än Health endpoint
2. **Betona typ-säkerhet** - Junior's `object` returtyp är perfekt exempel på brist på förståelse
3. **Visualisera lagerarkitekturen** - Använd diagram för att visa separation (eller brist på separation)
4. **Kvantifiera change impact** - Visa hur många filer som måste ändras för vanliga ändringar

### För analysen:

1. **Junior's största brister:**
   - Returnerar `object` från service (ingen typ-säkerhet)
   - HTTP-anrop i application layer (arkitekturfel)
   - Returnerar error som data (inte exceptions)
   - Ingen interface-användning (svår att testa)

2. **Senior's styrkor:**
   - Tydlig 3-lager arkitektur (Clean Architecture)
   - Typ-säkerhet genom hela stacken
   - Datatransformation mellan lager
   - Testbarhet på varje nivå
   - Strukturerad felhantering

3. **Novis' förvånande kvalitet:**
   - Använder `IHttpClientFactory` (best practice!)
   - Visar att rätt prompt kan ge rätt patterns
   - Men saknar totalt struktur för underhållbarhet

### För slutsatserna:

Jobs endpoint-analysen visar att:

- **Komplexitet avslöjar kompetens** - Enkla endpoints (Health) kan verka likvärdiga, men komplexa endpoints (Jobs) visar dramatiska skillnader
- **Typ-säkerhet är kritiskt** - Att returnera `object` är en fundamental brist som AI accepterade från Junior-prompt
- **Arkitektur kräver förståelse** - Att lägga HTTP-anrop i service-lager visar brist på Clean Architecture-förståelse
- **AI följer promptens nivå** - AI kommer inte "uppgradera" koden till bättre arkitektur än vad prompten beskriver
- **Transformation är nyckeln** - Senior's datatransformation mellan lager är skillnaden mellan tight coupling och loose coupling

Detta stödjer starkt er hypotes att **promptarens tekniska kompetens direkt reflekteras i kodkvaliteten**, särskilt när komplexiteten ökar.

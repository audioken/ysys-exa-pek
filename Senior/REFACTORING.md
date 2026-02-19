# Refaktorering av Senior-projektet

## Problem

Senior-projektet returnerade felmeddelandet **"Tjänsten är tillfälligt otillgänglig"** när `/jobs`-endpointen anropades, medan samma funktionalitet fungerade korrekt i Novis- och Junior-projekten.

## Rotorsaksanalys

Analysen identifierade två huvudproblem:

### Problem 1: HttpClient BaseAddress-konfiguration

**Symptom:**

- HTTP-anrop till externa API:et misslyckades
- `InvalidOperationException` kastades från `JobApiClient`

**Rotorsak:**
Projektet använde typed HttpClient pattern (`AddHttpClient<IJobApiClient, JobApiClient>()`), men försökte konfigurera `BaseAddress` i klassens konstruktor istället för vid dependency injection-registrering.

**Före:**

```csharp
// Program.cs
builder.Services.AddHttpClient<IJobApiClient, JobApiClient>();

// JobApiClient.cs
public JobApiClient(HttpClient httpClient, ILogger<JobApiClient> logger)
{
    _httpClient = httpClient;
    _logger = logger;
    _httpClient.BaseAddress = new Uri(BaseUrl); // Fungerar inte med typed clients
}
```

### Problem 2: JSON-deserialisering

**Symptom:**

```
System.Text.Json.JsonException: The JSON value could not be converted to System.Int32.
Path: $.total | Cannot get the value of a token type 'StartObject' as a number.
```

**Rotorsak:**
Arbetsförmedlingens API returnerar `total` som ett objekt med en `value`-property, inte som en direkt integer.

**API-respons:**

```json
{
  "total": {
    "value": 1234
  },
  "hits": [...]
}
```

**Före:**

```csharp
public class JobSearchResponse
{
    public int Total { get; set; }  // Förväntar sig ett heltal
    public List<JobSearchHit> Hits { get; set; } = new();
}
```

## Implementerad lösning

### Lösning 1: Flytta HttpClient-konfiguration till DI-registrering

**Program.cs:**

```csharp
// Registrera Job-relaterade services (Clean Architecture)
// Application layer
builder.Services.AddScoped<IJobService, JobService>();

// Infrastructure layer - HttpClient med typed client pattern
builder.Services.AddHttpClient<IJobApiClient, JobApiClient>(client =>
{
    client.BaseAddress = new Uri("https://jobsearch.api.jobtechdev.se/");
});
```

**JobApiClient.cs:**

```csharp
public JobApiClient(HttpClient httpClient, ILogger<JobApiClient> logger)
{
    _httpClient = httpClient;  // BaseAddress redan konfigurerad
    _logger = logger;
    // Rad för att sätta BaseAddress borttagen
}
```

### Lösning 2: Uppdatera datamodell för korrekt JSON-mappning

**JobSearchResponse.cs:**

```csharp
public class JobSearchResponse
{
    public TotalInfo? Total { get; set; }  // Nu ett objekt
    public List<JobSearchHit> Hits { get; set; } = new();
}

public class TotalInfo
{
    public int Value { get; set; }
}
```

## Resultat

Efter refaktoreringen:

✅ HTTP-anrop till Arbetsförmedlingens API fungerar korrekt

```
info: System.Net.Http.HttpClient.IJobApiClient.ClientHandler[101]
      Received HTTP response headers after 1447.9495ms - 200
```

✅ JSON-deserialisering lyckas utan fel

```
info: Senior.Services.JobService[0]
      Hämtade 5 jobb
```

✅ API-endpointen returnerar korrekt data istället för felmeddelande

## Lärdomar

1. **Modern .NET best practice**: När typed HttpClient pattern används (`AddHttpClient<TInterface, TImplementation>()`), ska all HttpClient-konfiguration inklusive BaseAddress göras vid DI-registrering i `Program.cs`, inte i implementationsklassens konstruktor.

2. **API-kontraktsvalidering**: Alltid verifiera det faktiska API-svaret mot datamodellerna. Externa API:er kan returnera komplexa objekt där enkla datatyper förväntas.

3. **Separation of Concerns**: Konfiguration hör hemma i startup-kod (Program.cs), inte i business logic-klasser. Detta förbättrar testbarhet och flexibilitet.

4. **Arkitekturmönster**: Även om Senior-projektets flerlagersarkitektur är mer komplex än Junior och Novis, följer den nu korrekt .NET-konventioner för dependency injection och HttpClient-hantering.

## Referenser

- [IHttpClientFactory in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory)
- [Typed clients with HttpClientFactory](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests#typed-clients)
- [System.Text.Json serialization](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/overview)

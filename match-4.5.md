# Jämförelse och analys av Match Endpoint - tre utvecklarnivåer

## Översikt av implementationerna

### **Novis-nivå**

- En controller med all matchningslogik direkt implementerad
- Models definierade som nested classes i controllern
- Tar emot **både kompetenser OCH jobb** i request-body
- Ingen extern datahämtning - klienten måste skicka allt
- Synkron matchningsalgoritm med string matching och similarity detection
- Total: ~151 rader kod i 1 fil

### **Junior-nivå**

- Controller som delegerar till en service
- Konkret `MatchService`-klass injicerad som koordinerar `CvService` och `JobsService`
- Tar endast **CV-text** i request - hämtar jobb från externt API
- Matchar CV-kompetenser mot jobbannonstexter
- Async med faktiska externa API-anrop
- Felhantering som returnerar tomma resultat
- Total: ~200 rader kod i 4 filer

### **Senior-nivå**

- Controller som delegerar till service via interface
- `IMatchingService` koordinerar `ICvAnalysisService`, `IJobService` och `IMatchingStrategy`
- **Strategy Pattern** för matchningsalgoritmer
- Rich request model med `MinimumMatchScore`, `MaxResults`, `SearchQuery`, `Location`, `JobIds`
- Detaljerad response med `MatchScore`, `MatchedSkills`, `MissingSkills`, `MatchExplanation`
- Sofistikerad matchningsalgoritm med frequency bonus och missing penalty
- ProblemDetails för strukturerade felmeddelanden
- Total: ~350+ rader kod i 7 filer

---

## Analys utifrån SOLID-principer

| **Princip** | **Novis**                                               | **Junior**                                                  | **Senior**                                                        |
| ----------- | ------------------------------------------------------- | ----------------------------------------------------------- | ----------------------------------------------------------------- |
| **SRP**     | ❌ Brister - controller gör allt (HTTP, matching, calc) | ⚠️ Delvis - men service gör för mycket (CV, Jobs, Matching) | ✅ Fullständig - varje service har ett tydligt ansvar             |
| **OCP**     | ❌ Omöjligt att ändra matchningsalgoritm                | ⚠️ Svårt - algoritm hårdkodad i service                     | ✅ Lätt - nya IMatchingStrategy kan läggas till                   |
| **LSP**     | N/A                                                     | N/A                                                         | ✅ Alla interfaces kan bytas ut utan sidoeffekter                 |
| **ISP**     | N/A                                                     | ⚠️ Ingen interface-segregation                              | ✅ Fokuserade interfaces för varje ansvar                         |
| **DIP**     | ❌ Ingen DI alls                                        | ⚠️ Beror på konkreta klasser                                | ✅ Beror på abstraktioner (ICvAnalysisService, IMatchingStrategy) |

---

## Analys utifrån Arkitekturprinciper

### **Separation of Concerns (SoC)**

**Novis:** Allvarlig brist på separation - controllern hanterar:

- HTTP request/response
- Validering av input
- Matchningsalgoritm
- String similarity detection
- Poängberäkning
- Sortering av resultat

```csharp
[HttpPost]
public IActionResult Match([FromBody] MatchRequest request)
{
    // ❌ Validering
    if (request?.Competencies == null || request.Competencies.Count == 0)
        return BadRequest("Competencies are required");

    // ❌ Matchningslogik
    var matches = CalculateMatches(request.Competencies, request.Jobs);

    // ❌ Response-mappning
    return Ok(new MatchResponse { Matches = matches, ... });
}

private List<JobMatch> CalculateMatches(...)
{
    // ❌ Komplex algoritm direkt i controller
    var matchedSkills = normalizedCompetencies
        .Where(comp => normalizedRequiredSkills.Any(skill =>
            skill.Contains(comp) || comp.Contains(skill) ||
            AreSimilarSkills(comp, skill)))
        .ToList();
}
```

**Junior:** Bättre separation med service-lager, men servicen gör för mycket:

```csharp
public async Task<MatchResponse> MatchCvToJobsAsync(string cvText)
{
    // 1. Extrahera kompetenser från CV
    var cvAnalysis = await _cvService.AnalyzeCvAsync(cvText);

    // 2. Hämta jobbannonser
    var jobsData = await _jobsService.GetJobsAsync();
    var jobs = ParseJobsFromResponse(jobsData);  // ❌ Parsing-logik i matching service

    // 3. Matcha kompetenser
    foreach (var job in jobs)
    {
        var jobContent = $"{job.Title} {job.Description}".ToLower();
        // ❌ Matchningsalgoritm hårdkodad
        foreach (var skill in cvSkills)
        {
            if (jobContent.Contains(skill.ToLower()))
                matchedSkills.Add(skill);
        }
    }
}
```

Problem:

- Service har tre olika ansvarsområden (CV-analys koordinering, jobbhämtning/parsing, matchning)
- Matchningsalgoritmen är hårdkodad - omöjligt att byta utan att ändra servicen
- Parsing av externa API-data i matchningsservice (bör vara i JobsService)

**Senior:** Exemplarisk separation med tydliga ansvarsområden:

1. **Presentation Layer (Controller)** - HTTP-hantering, validering, ProblemDetails
2. **Application Layer (MatchingService)** - Koordinerar subservices, aggregerar resultat
3. **Domain Layer (MatchingStrategy)** - Matchningsalgoritm isolerad i Strategy
4. **Infrastructure Layer (JobApiClient)** - Extern datahämtning (via IJobService)

```csharp
// Controller - endast HTTP concerns
public async Task<IActionResult> MatchCvWithJobs([FromBody] MatchRequest request)
{
    // Validering
    if (request.MinimumMatchScore < 0 || request.MinimumMatchScore > 100)
        return BadRequest(new ProblemDetails { ... });

    // Delegera till service
    var response = await _matchingService.MatchCvWithJobsAsync(request);
    return Ok(response);
}

// MatchingService - koordinerar process
public async Task<MatchResponse> MatchCvWithJobsAsync(MatchRequest request)
{
    // 1. Extrahera kompetenser
    var cvAnalysis = await _cvAnalysisService.AnalyzeCvAsync(...);

    // 2. Hämta jobb (delegation till IJobService)
    var jobs = await GetJobsToMatchAsync(request);

    // 3. Matcha med strategi (delegation till IMatchingStrategy)
    var matchTasks = jobs.Select(job => _matchingStrategy.CalculateMatchAsync(cvSkills, job));
    var matches = await Task.WhenAll(matchTasks);

    // 4. Filtrera och sortera
    return new MatchResponse { ... };
}

// MatchingStrategy - isolerad algoritm
public Task<MatchResult> CalculateMatchAsync(List<string> cvSkills, JobDto job)
{
    // Matchningslogik här, separat från service
}
```

Varje lager har sitt tydliga ansvar.

### **Design Patterns**

**Novis:** Inga design patterns - monolitisk controller med all logik.

**Junior:** Inga design patterns - service som koordinerar andra services men utan abstraktion.

**Senior:** Implementerar **Strategy Pattern** för matchningsalgoritmer:

```csharp
public interface IMatchingStrategy
{
    string StrategyName { get; }
    Task<MatchResult> CalculateMatchAsync(List<string> cvSkills, JobDto job);
}

public class SkillBasedMatchingStrategy : IMatchingStrategy
{
    public string StrategyName => "Skill-Based Matching";

    public Task<MatchResult> CalculateMatchAsync(List<string> cvSkills, JobDto job)
    {
        // Komplex algoritm:
        // - Frequency bonus för multipla förekomster
        // - Missing penalty för saknade kompetenser
        // - Score clamping och normalisering
        // - Detaljerad förklaring
    }
}
```

Fördelar:

- **Multiple algoritmer** kan implementeras (KeywordBased, SemanticBased, MLBased)
- **A/B-testning** enkelt genom att byta strategi
- **Algoritm-specifik logik** isolerad från koordinering
- **Open/Closed** - nya strategier utan att ändra MatchingService

### **Request/Response Design**

**Novis:** Klienten måste skicka allt - både kompetenser OCH jobb:

```csharp
public class MatchRequest
{
    public List<string> Competencies { get; set; } = new();
    public List<JobData> Jobs { get; set; } = new();  // ❌ Klient måste hämta jobb själv
}
```

Problem:

- Klienten ansvarar för att hämta jobb från externt API
- Ingen server-side filtering eller enrichment
- Endpoint blir "bara en function" - ingen värdeskapande logik

**Junior:** Tar endast CV-text - hämtar jobb automatiskt:

```csharp
public class MatchRequest
{
    public string CvText { get; set; } = string.Empty;  // ✅ Endast CV-text
}
```

Bättre, men:

- Inga parametrar för att styra jobbsökning
- Inga parametrar för att filtrera matchningar
- Begränsad kontroll för klienten

**Senior:** Rich request model med omfattande parametrar:

```csharp
public class MatchRequest
{
    public string CvText { get; set; } = string.Empty;
    public List<string>? JobIds { get; set; }           // Matcha mot specifika jobb
    public string? SearchQuery { get; set; }            // Sökord för jobbhämtning
    public string? Location { get; set; }               // Platsfilter
    public int? RadiusKm { get; set; }                  // Geografisk radie
    public int MinimumMatchScore { get; set; } = 0;     // Filtrera låga matchningar
    public int MaxResults { get; set; } = 10;           // Begränsa antal resultat
}
```

Fördelar:

- **Flexibel** - klient kan styra både sökning och filtrering
- **Server-side optimization** - filtrering sker innan data skickas
- **Multiple use cases** - kan användas för specifika jobb eller bredsökning

### **Response Design - Detaljnivå**

**Novis:** Basic matchning med poäng:

```csharp
public class JobMatch
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public List<string> RequiredSkills { get; set; }    // ❌ Hårdkodad lista (klient skickar)
    public List<string> MatchedSkills { get; set; }
    public int MatchScore { get; set; }                  // Antal matchade
    public double MatchPercentage { get; set; }          // Procent
}
```

**Junior:** Liknande struktur men dynamiska jobb:

```csharp
public class JobMatch
{
    public string JobTitle { get; set; }
    public string Employer { get; set; }
    public string Description { get; set; }
    public List<string> MatchedSkills { get; set; }
    public double MatchScore { get; set; }              // Procent av CV-skills
    public string JobUrl { get; set; }
}

public class MatchResponse
{
    public List<JobMatch> Matches { get; set; }
    public int TotalMatches { get; set; }
    public List<string> IdentifiedSkills { get; set; }  // ✅ Visar extraherade skills
}
```

**Senior:** Omfattande detaljerad response:

```csharp
public class MatchResult
{
    public JobDto Job { get; set; }                      // Fullständig jobb-information
    public int MatchScore { get; set; }                  // 0-100 normaliserad poäng
    public List<string> MatchedSkills { get; set; }      // Skills som matchar
    public List<string> MissingSkills { get; set; }      // ✅ Skills från jobb som saknas i CV
    public string MatchExplanation { get; set; }         // ✅ Human-readable förklaring
}

public class MatchResponse
{
    public List<MatchResult> Matches { get; set; }
    public int TotalJobsEvaluated { get; set; }          // ✅ Visar hur många som utvärderades
    public List<string> ExtractedSkills { get; set; }
    public string MatchingStrategy { get; set; }         // ✅ Vilken algoritm som användes
}
```

Fördelar:

- **MissingSkills** hjälper användare förstå vad de behöver lära sig
- **MatchExplanation** gör algoritmen transparent ("Matchar 6 av 10 kompetenser...")
- **MatchingStrategy** gör det tydligt vilken algoritm som kördes
- **TotalJobsEvaluated** ger kontext (10 matchningar av 50 utvärderade jobb)

---

## Matchningsalgoritm - Komplexitet och sofistikering

### **Novis: Enkel string matching med similarity detection**

```csharp
private List<JobMatch> CalculateMatches(List<string> competencies, List<JobData> jobs)
{
    var normalizedCompetencies = competencies
        .Select(c => c.Trim().ToLower())
        .Distinct()
        .ToList();

    foreach (var job in jobs)
    {
        var normalizedRequiredSkills = job.RequiredSkills
            .Select(s => s.Trim().ToLower())
            .Distinct()
            .ToList();

        // Enkel matching: exact match, contains, eller similarity
        var matchedSkills = normalizedCompetencies
            .Where(comp => normalizedRequiredSkills.Any(skill =>
                skill.Contains(comp) || comp.Contains(skill) ||
                AreSimilarSkills(comp, skill)))  // ✅ Har similarity detection!
            .ToList();

        int matchScore = matchedSkills.Count;
        double matchPercentage = normalizedRequiredSkills.Count > 0
            ? (double)matchScore / normalizedRequiredSkills.Count * 100
            : 0;
    }
}

private bool AreSimilarSkills(string skill1, string skill2)
{
    // Hårdkodad mapping av skill-variationer
    var skillMappings = new Dictionary<string, List<string>>
    {
        { "javascript", new List<string> { "js", "ecmascript" } },
        { "c#", new List<string> { "csharp", "c sharp" } },
        // ...
    };
}
```

**Styrkor:**

- Har faktiskt similarity detection (JS = JavaScript)
- Normalisering av case och whitespace

**Svagheter:**

- Hårdkodad skill mapping - omöjligt att utöka
- Enkel algoritm utan frequency consideration
- Ingen missing skills detection
- Ingen explanation

### **Junior: String contains-baserad matching**

```csharp
foreach (var job in jobs)
{
    var jobContent = $"{job.Title} {job.Description}".ToLower();
    var matchedSkills = new List<string>();

    foreach (var skill in cvSkills)
    {
        if (jobContent.Contains(skill.ToLower()))  // ❌ Mycket enkel
        {
            matchedSkills.Add(skill);
        }
    }

    if (matchedSkills.Count > 0)
    {
        double matchScore = (double)matchedSkills.Count / cvSkills.Count * 100;
        matches.Add(new JobMatch { ... });
    }
}
```

**Styrkor:**

- Matchar mot hela jobbtexten (titel + beskrivning)
- Filterar bort jobb utan matchningar

**Svagheter:**

- Väldigt enkel string.Contains - falskt positiva (t.ex. "Go" matchar "Google")
- Ingen normalisering eller similarity
- Ingen consideration för hur många gånger skill förekommer
- Ingen missing skills detection
- Ingen explanation

### **Senior: Sofistikerad algoritm med multiple faktorer**

```csharp
public Task<MatchResult> CalculateMatchAsync(List<string> cvSkills, JobDto job)
{
    var jobText = $"{job.Headline} {job.Description}".ToLowerInvariant();

    // 1. Hitta matchade kompetenser
    var matchedSkills = new List<string>();
    var skillOccurrences = new Dictionary<string, int>();

    foreach (var skill in cvSkills)
    {
        var count = CountOccurrences(jobText, skill.ToLowerInvariant());  // ✅ Räknar frekvens
        if (count > 0)
        {
            matchedSkills.Add(skill);
            skillOccurrences[skill] = count;
        }
    }

    // 2. Extrahera saknade kompetenser från jobbet
    var missingSkills = ExtractMissingSkills(jobText, cvSkills);  // ✅ Missing skills!

    // 3. Beräkna poäng med multiple faktorer
    var baseScore = cvSkills.Count > 0
        ? (matchedSkills.Count * 100) / cvSkills.Count
        : 0;

    var frequencyBonus = skillOccurrences.Values.Sum(c => Math.Min(c - 1, 5));  // ✅ Bonus för frekvens
    var missingPenalty = Math.Min(missingSkills.Count * 5, 30);                 // ✅ Penalty för saknade

    result.MatchScore = Math.Clamp(baseScore + frequencyBonus - missingPenalty, 0, 100);

    // 4. Generera förklaring
    result.MatchExplanation = GenerateExplanation(
        matchedSkills.Count,
        missingSkills.Count,
        cvSkills.Count,
        skillOccurrences);  // ✅ Human-readable explanation
}

private string GenerateExplanation(...)
{
    var matchPercentage = totalCvSkills > 0
        ? (matchedCount * 100) / totalCvSkills
        : 0;

    var explanation = $"Matchar {matchedCount} av {totalCvSkills} kompetenser ({matchPercentage}%).";

    if (occurrences.Any(o => o.Value > 1))
    {
        var topSkills = occurrences
            .OrderByDescending(o => o.Value)
            .Take(3)
            .Select(o => $"{o.Key} ({o.Value}x)");
        explanation += $" Nyckelkompetenser: {string.Join(", ", topSkills)}.";
    }

    if (missingCount > 0)
    {
        explanation += $" {missingCount} efterfrågade kompetenser saknas i CV.";
    }

    return explanation;
}
```

**Styrkor:**

- **Frequency counting** - skills som nämns flera gånger värderas högre
- **Missing skills detection** - visar vad användaren behöver lära sig
- **Contextuell poängberäkning** - base score + bonus - penalty
- **Explanation generation** - transparent algoritm
- **Score normalization** - alltid 0-100 med Math.Clamp

**Exempel på explanation:**

> "Matchar 6 av 10 kompetenser (60%). Nyckelkompetenser: C# (5x), Docker (3x), Kubernetes (2x). 3 efterfrågade kompetenser saknas i CV."

---

## Testbarhet

| **Aspekt**                        | **Novis**                             | **Junior**                                        | **Senior**                               |
| --------------------------------- | ------------------------------------- | ------------------------------------------------- | ---------------------------------------- |
| Unit-testning av controller       | ⚠️ Kan testas men all logik finns här | ⚠️ Måste mocka flera konkreta services            | ✅ Enkelt - mocka IMatchingService       |
| Unit-testning av matchningslogik  | ❌ Inbäddad i controller              | ⚠️ Inbäddad i service med externa anrop           | ✅ Isolerad i IMatchingStrategy          |
| Unit-testning av algoritm         | ❌ Svårt - blandat med HTTP           | ❌ Svårt - blandat med CV-analys och jobbhämtning | ✅ Trivial - ren funktion                |
| Testning av ny matchningsalgoritm | ❌ Måste ändra controller             | ❌ Måste ändra service                            | ✅ Skapa ny IMatchingStrategy            |
| Mockning av dependencies          | N/A                                   | ⚠️ Måste mocka konkreta CvService & JobsService   | ✅ Mocka ICvAnalysisService, IJobService |
| Integration-testning              | ❌ Allt måste testas tillsammans      | ⚠️ Komplext - många dependencies                  | ✅ Tydliga gränser                       |

### **Testexempel - Novis (Mycket svårt)**

```csharp
// Måste testa hela controllern inklusive algoritm
var controller = new MatchController();
var request = new MatchController.MatchRequest
{
    Competencies = new List<string> { "C#", "JavaScript" },
    Jobs = new List<MatchController.JobData>
    {
        new() { Title = "C# Developer", RequiredSkills = new List<string> { "C#", "SQL" } }
    }
};

var result = controller.Match(request) as OkObjectResult;
var response = result.Value as MatchController.MatchResponse;

// ❌ Testar HTTP-hantering OCH matchningsalgoritm samtidigt
// ❌ Svårt att testa edge cases i algoritmen isolerat
// ❌ Måste skapa hela jobbobjekt för varje test
```

### **Testexempel - Junior (Svårt)**

```csharp
// Måste mocka flera konkreta services
var mockCvService = new Mock<CvService>();  // ❌ Konkret klass
var mockJobsService = new Mock<JobsService>();
var mockLogger = new Mock<ILogger<MatchService>>();

// Eftersom CvService & JobsService är konkreta klasser utan virtual methods,
// går det inte att mocka dem enkelt. Skulle kräva interface-baserad DI.

// Alternativt - integration test med riktiga services
var cvService = new CvService(mockCvLogger.Object);
var jobsService = new JobsService(httpClient, mockJobLogger.Object);
var matchService = new MatchService(cvService, jobsService, mockLogger.Object);

// ⚠️ Integration test istället för unit test
// ⚠️ Svårt att testa matchningsalgoritmen isolerat
```

### **Testexempel - Senior (Enkelt)**

**Test av matchningsstrategi isolerat:**

```csharp
var logger = new Mock<ILogger<SkillBasedMatchingStrategy>>();
var strategy = new SkillBasedMatchingStrategy(logger.Object);

var cvSkills = new List<string> { "C#", "Docker", "Kubernetes" };
var job = new JobDto
{
    Headline = "Senior C# Developer",
    Description = "Looking for C# expert with Docker and cloud experience. C# and Docker are critical skills."
};

var result = await strategy.CalculateMatchAsync(cvSkills, job);

Assert.Equal(3, result.MatchedSkills.Count);
Assert.Contains("C#", result.MatchedSkills);
Assert.True(result.MatchScore > 50);  // Base score + frequency bonus
Assert.Contains("Matchar 3 av 3 kompetenser", result.MatchExplanation);
// ✅ Testar endast algoritm-logik, ingen HTTP eller external dependencies
```

**Test av koordinering i MatchingService:**

```csharp
var mockCvService = new Mock<ICvAnalysisService>();
mockCvService.Setup(s => s.AnalyzeCvAsync(It.IsAny<CvAnalysisRequest>()))
    .ReturnsAsync(new CvAnalysisResponse
    {
        TechnicalSkills = new List<string> { "Docker", "Kubernetes" },
        ProgrammingLanguages = new List<string> { "C#" }
    });

var mockJobService = new Mock<IJobService>();
mockJobService.Setup(s => s.GetJobsAsync(It.IsAny<string>(), It.IsAny<int>()))
    .ReturnsAsync(new List<JobDto> { /* test jobs */ });

var mockStrategy = new Mock<IMatchingStrategy>();
mockStrategy.Setup(s => s.CalculateMatchAsync(It.IsAny<List<string>>(), It.IsAny<JobDto>()))
    .ReturnsAsync(new MatchResult { MatchScore = 75 });

var service = new MatchingService(
    mockCvService.Object,
    mockJobService.Object,
    mockStrategy.Object,
    mockLogger.Object);

var request = new MatchRequest { CvText = "Test CV", MinimumMatchScore = 50 };
var response = await service.MatchCvWithJobsAsync(request);

// ✅ Testar koordinering utan att bry sig om implementation av subservices
// ✅ Verifierar att services anropas i rätt ordning
mockCvService.Verify(s => s.AnalyzeCvAsync(It.IsAny<CvAnalysisRequest>()), Times.Once);
mockJobService.Verify(s => s.GetJobsAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
```

**Test av controller:**

```csharp
var mockService = new Mock<IMatchingService>();
mockService.Setup(s => s.MatchCvWithJobsAsync(It.IsAny<MatchRequest>()))
    .ReturnsAsync(new MatchResponse
    {
        Matches = new List<MatchResult> { /* test matches */ }
    });

var controller = new MatchController(mockService.Object, mockLogger.Object);

var request = new MatchRequest { CvText = "Test", MinimumMatchScore = 150 };  // Invalid
var result = await controller.MatchCvWithJobs(request) as BadRequestObjectResult;

Assert.NotNull(result);
var problem = result.Value as ProblemDetails;
Assert.Contains("MinimumMatchScore måste vara mellan 0 och 100", problem.Detail);
// ✅ Testar endast validering och HTTP-hantering
```

---

## Underhållbarhet och utbyggbarhet

### **Scenario 1: Implementera semantisk matching med ML-modell**

**Novis:**

- Måste skriva om hela `CalculateMatches`-metoden i controllern ❌
- Risk för regression i HTTP-hantering ❌
- Omöjligt att köra både gamla och nya algoritmen för jämförelse ❌

**Junior:**

- Måste skriva om matchningslogiken i `MatchService` ❌
- Svårt att A/B-testa gamla vs nya algoritmen ❌
- Måste ändra befintlig kod (bryter OCP) ❌

**Senior:**

- Skapa ny `SemanticMatchingStrategy : IMatchingStrategy` ✅
- Registrera i DI-container ✅
- Ingen ändring i MatchingService eller Controller ✅
- Kan köra båda för A/B-testning ✅

```csharp
public class SemanticMatchingStrategy : IMatchingStrategy
{
    private readonly IMLModel _mlModel;

    public string StrategyName => "Semantic ML-Based Matching";

    public SemanticMatchingStrategy(IMLModel mlModel)
    {
        _mlModel = mlModel;
    }

    public async Task<MatchResult> CalculateMatchAsync(List<string> cvSkills, JobDto job)
    {
        // Använd ML-modell för semantisk matchning
        var semanticScore = await _mlModel.CalculateSimilarityAsync(cvSkills, job.Description);

        return new MatchResult
        {
            Job = job,
            MatchScore = (int)(semanticScore * 100),
            MatchExplanation = $"Semantisk likhet: {semanticScore:P0} (ML-baserad)"
        };
    }
}

// I Program.cs - byt strategi
builder.Services.AddScoped<IMatchingStrategy, SemanticMatchingStrategy>();

// Eller kör båda för jämförelse
builder.Services.AddScoped<IMatchingStrategy, SkillBasedMatchingStrategy>();
builder.Services.AddScoped<IMatchingStrategy, SemanticMatchingStrategy>();
// MatchingService kan köra båda och kombinera resultaten
```

### **Scenario 2: Lägg till geografisk filtrering**

**Novis:**

- Klienten skickar redan filtrerade jobb, så ingen server-side filtrering ❌
- Måste ändra klientkod för att filtrera innan anrop ❌

**Junior:**

- Måste ändra `GetJobsAsync` för att ta location-parametrar ⚠️
- Parsar redan jobbdata i MatchService - kan lägga till filtrering här ⚠️
- Men blir ännu mer "god klass" ⚠️

**Senior:**

- Request har redan `Location` och `RadiusKm` parametrar ✅
- `GetJobsToMatchAsync` kan filtrera baserat på dessa ✅
- Ingen ändring i matchningsalgoritm eller controller ✅

```csharp
private async Task<List<JobDto>> GetJobsToMatchAsync(MatchRequest request)
{
    var jobs = await _jobService.GetJobsAsync(request.SearchQuery, limit);

    // Filtrera på location om angivet
    if (!string.IsNullOrEmpty(request.Location) && request.RadiusKm.HasValue)
    {
        jobs = jobs.Where(j => IsWithinRadius(j.Location, request.Location, request.RadiusKm.Value))
                   .ToList();
    }

    // Filtrera på specifika jobb-ID:n om angivet
    if (request.JobIds != null && request.JobIds.Any())
    {
        jobs = jobs.Where(j => request.JobIds.Contains(j.Id)).ToList();
    }

    return jobs;
}
```

### **Scenario 3: Lägg till caching av matchningsresultat**

**Novis:** Omöjligt utan att ändra controllern ❌

**Junior:** Kan lägga till caching i service, men kompliceras av externa anrop ⚠️

**Senior:** Dekorera `IMatchingService` med caching-implementation ✅

```csharp
public class CachedMatchingService : IMatchingService
{
    private readonly IMatchingService _innerService;
    private readonly IMemoryCache _cache;

    public CachedMatchingService(IMatchingService innerService, IMemoryCache cache)
    {
        _innerService = innerService;
        _cache = cache;
    }

    public async Task<MatchResponse> MatchCvWithJobsAsync(MatchRequest request)
    {
        var cacheKey = GenerateCacheKey(request);

        if (_cache.TryGetValue(cacheKey, out MatchResponse cachedResponse))
            return cachedResponse;

        var response = await _innerService.MatchCvWithJobsAsync(request);

        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(15));
        return response;
    }
}

// I Program.cs - registrera dekorerad service
builder.Services.AddScoped<MatchingService>();
builder.Services.AddScoped<IMatchingService, CachedMatchingService>();
```

**Decorator Pattern** respekterar OCP - ingen ändring i befintlig kod.

---

## Felhantering och Validering

### **Novis:** Basic validering i controller

```csharp
if (request?.Competencies == null || request.Competencies.Count == 0)
{
    return BadRequest("Competencies are required");
}

if (request.Jobs == null || request.Jobs.Count == 0)
{
    return BadRequest("Jobs list is required");
}

// ❌ Ingen try-catch för oväntade fel
```

**Junior:** Try-catch som returnerar tomt resultat

```csharp
try
{
    var cvAnalysis = await _cvService.AnalyzeCvAsync(cvText);
    if (cvSkills.Count == 0)
    {
        _logger.LogWarning("No skills found in CV text");
        return new MatchResponse  // ⚠️ Returnerar tomt resultat
        {
            Matches = new List<JobMatch>(),
            TotalMatches = 0,
            IdentifiedSkills = new List<string>()
        };
    }
    // ...
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error occurred while matching CV to jobs");
    return new MatchResponse  // ❌ Swallows exception
    {
        Matches = new List<JobMatch>(),
        TotalMatches = 0,
        IdentifiedSkills = new List<string>()
    };
}
```

Problem:

- Klienten får tomt resultat utan att veta om det var fel eller inga matchningar
- Ingen distinktion mellan olika feltyper

**Senior:** Omfattande validering med ProblemDetails

```csharp
// Controller - detaljerad validering
if (request == null)
{
    return BadRequest(new ProblemDetails
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "Ogiltig förfrågan",
        Detail = "Request-objektet saknas"
    });
}

if (string.IsNullOrWhiteSpace(request.CvText))
{
    return BadRequest(new ProblemDetails
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "Ogiltig förfrågan",
        Detail = "CV-text är obligatorisk"
    });
}

if (request.MinimumMatchScore < 0 || request.MinimumMatchScore > 100)
{
    return BadRequest(new ProblemDetails
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "Ogiltig förfrågan",
        Detail = "MinimumMatchScore måste vara mellan 0 och 100"
    });
}

if (request.MaxResults < 1 || request.MaxResults > 100)
{
    return BadRequest(new ProblemDetails
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "Ogiltig förfrågan",
        Detail = "MaxResults måste vara mellan 1 och 100"
    });
}

try
{
    var response = await _matchingService.MatchCvWithJobsAsync(request);
    return Ok(response);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Fel vid matchning av CV mot jobb");
    return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
    {
        Status = StatusCodes.Status500InternalServerError,
        Title = "Internt serverfel",
        Detail = "Ett oväntat fel inträffade vid matchning av CV mot jobb"
    });
}
```

Fördelar:

- **ProblemDetails** följer RFC 7807-standard
- Specifika felmeddelanden för varje valideringsregel
- Strukturerad felhantering med lämpliga statuskoder
- Logging för monitoring och debugging

---

## Dependency Graph - Komplexitet

### **Novis - Inga dependencies**

```
┌─────────────────────────────┐
│   MatchController           │
│   - All logic here          │
│   - No dependencies         │
└─────────────────────────────┘
```

**Fördelar:** Extremt enkel
**Nackdelar:** Omöjligt att testa eller underhålla

### **Junior - Multiple concrete dependencies**

```
┌─────────────────────────────┐
│   MatchController           │
└────────┬────────────────────┘
         │ (concrete)
         ▼
┌─────────────────────────────┐
│   MatchService              │
├─────────┬───────────┬───────┤
│         │           │       │
│    CvService   JobsService  │
│    (concrete)  (concrete)   │
└─────────────────────────────┘
```

**Fördelar:** Separation av concerns
**Nackdelar:**

- Konkreta dependencies - svårt att mocka
- "God klass" som gör för mycket
- Hårdkodad algoritm

### **Senior - Interface-based dependency graph**

```
┌──────────────────────────────────────┐
│   Presentation Layer                 │
│   (MatchController)                  │
└─────────┬────────────────────────────┘
          │ IMatchingService
          ▼
┌──────────────────────────────────────┐
│   Application Layer                  │
│   (MatchingService)                  │
├──────┬────────┬──────────────────────┤
│      │        │                      │
│   ICvAnalysisService                 │
│      │     IJobService                │
│      │        │    IMatchingStrategy  │
└──────┴────────┴──────────┬───────────┘
                           │
          ┌────────────────┼────────────────┐
          ▼                ▼                ▼
    SkillBasedStrategy  SemanticStrategy  KeywordStrategy
```

**Fördelar:**

- Alla dependencies via interfaces
- Strategy Pattern för algoritmer
- Enkelt att mocka och testa
- Kan utökas utan att ändra befintlig kod

---

## Kodens läsbarhet och Intent

### **Novis:**

```csharp
var matchedSkills = normalizedCompetencies
    .Where(comp => normalizedRequiredSkills.Any(skill =>
        skill.Contains(comp) || comp.Contains(skill) ||
        AreSimilarSkills(comp, skill)))
    .ToList();

int matchScore = matchedSkills.Count;
double matchPercentage = normalizedRequiredSkills.Count > 0
    ? (double)matchScore / normalizedRequiredSkills.Count * 100
    : 0;
```

Problem:

- Komplex LINQ utan kommentarer
- Otydligt vad `AreSimilarSkills` gör
- Matchningslogik blandat med HTTP-hantering

### **Junior:**

```csharp
var jobContent = $"{job.Title} {job.Description}".ToLower();

foreach (var skill in cvSkills)
{
    if (jobContent.Contains(skill.ToLower()))
    {
        matchedSkills.Add(skill);
    }
}

double matchScore = (double)matchedSkills.Count / cvSkills.Count * 100;
```

Enklare men:

- Mycket basic algoritm
- Ingen förklaring av varför denna approach valdes
- Ingen hantering av edge cases

### **Senior:**

```csharp
// 1. Extrahera kompetenser från CV
var cvAnalysis = await _cvAnalysisService.AnalyzeCvAsync(cvAnalysisRequest);
var cvSkills = new List<string>();
cvSkills.AddRange(cvAnalysis.TechnicalSkills);
cvSkills.AddRange(cvAnalysis.SoftSkills);
// ...

// 2. Hämta relevanta jobb
var jobs = await GetJobsToMatchAsync(request);

// 3. Beräkna matchningar för varje jobb med vald strategi
var matchTasks = jobs.Select(job => _matchingStrategy.CalculateMatchAsync(cvSkills, job));
var matches = await Task.WhenAll(matchTasks);  // ✅ Parallel execution

// 4. Filtrera och sortera resultat
var filteredMatches = matches
    .Where(m => m.MatchScore >= request.MinimumMatchScore)
    .OrderByDescending(m => m.MatchScore)
    .Take(request.MaxResults)
    .ToList();
```

Tydlig intent:

- Numrerade steg gör processen självdokumenterande
- Parallell exekvering med Task.WhenAll för prestanda
- Tydliga metodnamn (`GetJobsToMatchAsync`, `CalculateMatchAsync`)
- XML-kommentarer på alla publika metoder

---

## Uppfyllnad av examensarbetets kriterier

### **Novis - "Funktion över struktur"**

✅ **Uppfyller förväntningarna:**

- Regelstyrt: Implementerar basic matchning
- Begränsad situationsförståelse: Ingen arkitektonisk medvetenhet
- Fokus på funktion: Matchar kompetenser som efterfrågats
- Intressant twist: Har faktiskt similarity detection (JS = JavaScript)
- Men: Klienten måste göra allt jobbhämtningsarbete själv

**Bedömning:** "Calculator function" - fungerar men ger inget server-side värde.

### **Junior - "Struktur men utan djup arkitektonisk kontroll"**

⚠️ **Uppfyller delvis, med betydande brister:**

- Situationsförståelse: Förstår att koordinera multiple services ✅
- Tillämpar riktlinjer: Försöker separera concerns ✅
- Men: Service är "god klass" som gör för mycket ❌
- Men: Hårdkodad matchningsalgoritm ❌
- Men: Swallows exceptions ❌
- Men: Ingen interface-användning ❌
- Men: Parsing av externa data i fel lager ❌

**Bedömning:** Utvecklare som "förstår att delegera men inte hur man strukturerar för utbyggnad".

### **Senior - "Arkitektur, underhållbarhet och kodkvalitet"**

✅ **Uppfyller förväntningarna helt:**

- Helhetsförståelse: Clean Architecture med Strategy Pattern
- SOLID-principer: Alla tillämpade korrekt
- Design Patterns: Strategy Pattern för matchningsalgoritmer
- Rich domain modeling: Detaljerade request/response med multiple parametrar
- Advanced algorithm: Frequency bonus, missing penalty, explanation generation
- Parallel execution: Task.WhenAll för prestanda
- ProblemDetails: RFC 7807-standard för felhantering
- Comprehensive validation: Detaljerad validering av alla parametrar
- Testbarhet: Fullständigt mockbar på varje nivå
- Open/Closed: Nya strategier kan läggas till utan ändringar

**Bedömning:** Enterprise-standard med fokus på flexibilitet, underhållbarhet och användarupplevelse.

---

## Sammanfattande kvalitetsbedömning

| **Kriterium**           | **Novis**   | **Junior**  | **Senior**  |
| ----------------------- | ----------- | ----------- | ----------- |
| Funktionalitet          | ✅ Fungerar | ✅ Fungerar | ✅ Fungerar |
| SOLID-principer         | 0/5         | 1/5         | 5/5         |
| Design Patterns         | 0/5         | 0/5         | 5/5         |
| Arkitektur (Lager)      | 0/5         | 2/5         | 5/5         |
| Testbarhet              | 0/5         | 1/5         | 5/5         |
| Underhållbarhet         | 0/5         | 2/5         | 5/5         |
| Utbyggbarhet (OCP)      | 0/5         | 1/5         | 5/5         |
| Matchningsalgoritm      | 2/5         | 1/5         | 5/5         |
| Request/Response Design | 1/5         | 3/5         | 5/5         |
| Felhantering            | 1/5         | 1/5         | 5/5         |
| Användarupplevelse (UX) | 2/5         | 3/5         | 5/5         |
| **Totalt**              | **6/55**    | **15/55**   | **55/55**   |

---

## Kritiska observationer för examensarbetet

### **1. Strategy Pattern är avgörande för matchningsalgoritmer**

Matchning är ett perfekt användningsområde för Strategy Pattern:

- **Multiple algoritmer finns:** Keyword-based, semantic, ML-based, rule-based
- **A/B-testning behövs:** Jämför olika algoritmer för att hitta bästa
- **Domänexperter kan bidra:** Nya strategier baserat på användarfeedback

Novis och Junior har hårdkodade algoritmer - **omöjligt att experimentera**. Senior kan lägga till ny strategi på 30 rader kod utan att ändra något annat.

### **2. "God klass" är ett vanligt anti-pattern**

Junior's `MatchService` gör ALLT:

- Koordinerar CV-analys
- Hämtar jobb
- Parsar extern API-data
- Implementerar matchningsalgoritm
- Beräknar poäng
- Sorterar resultat

Detta är en klassisk "god klass" som bryter mot SRP. När något ska ändras, måste denna klass ändras.

Senior separerar varje ansvar:

- `MatchingService` - koordinerar endast
- `CvAnalysisService` - hanterar CV-analys
- `JobService` - hanterar jobbhämtning
- `MatchingStrategy` - implementerar algoritm

### **3. MissingSkills är en game-changer för användarupplevelse**

Senior's respons inkluderar `MissingSkills` - kompetenser som efterfrågas i jobbet men saknas i CV:

```csharp
public class MatchResult
{
    public List<string> MatchedSkills { get; set; }     // C#, Docker
    public List<string> MissingSkills { get; set; }     // Kubernetes, Azure  ← ✅ Hjälper användare!
    public string MatchExplanation { get; set; }         // Human-readable text
}
```

Detta gör att användare kan:

- Se vad de behöver lära sig för att få jobbet
- Prioritera vidareutbildning
- Förstå varför matchningspoängen är låg

Junior och Novis visar endast matchade kompetenser - **ingen vägledning** för förbättring.

### **4. Frequency counting är viktigare än man tror**

Senior's algoritm räknar hur många gånger varje kompetens nämns i jobbannonsen:

```csharp
var skillOccurrences = new Dictionary<string, int>();
foreach (var skill in cvSkills)
{
    var count = CountOccurrences(jobText, skill.ToLowerInvariant());
    skillOccurrences[skill] = count;
}

var frequencyBonus = skillOccurrences.Values.Sum(c => Math.Min(c - 1, 5));
```

Om "C#" nämns 5 gånger i annonsen, är det viktigare än en skill som nämns en gång. Detta ger **mycket mer realistiska matchningspoäng**.

Junior's enkel `Contains` ger samma vikt till alla skills.

### **5. Server-side vs client-side responsibilities**

**Novis** kräver att klienten:

- Hämtar jobb från API
- Skickar alla jobb i request
- Gör basisk matchning själv

Detta är **fel arkitektur** - servern blir en "dumb calculator".

**Senior** kräver endast CV-text från klient och:

- Hämtar relevanta jobb server-side
- Filtrerar baserat på SearchQuery, Location
- Gör sofistikerad matchning
- Returnerar endast top matchningar

Detta är **korrekt arkitektur** - servern ger värde genom intelligens och optimering.

### **6. ProblemDetails vs enkla strings**

**Novis & Junior:** Enkla error strings

```csharp
return BadRequest("CV text is required");
```

**Senior:** RFC 7807 ProblemDetails

```csharp
return BadRequest(new ProblemDetails
{
    Status = StatusCodes.Status400BadRequest,
    Title = "Ogiltig förfrågan",
    Detail = "CV-text är obligatorisk",
    Type = "https://api.example.com/errors/missing-cv-text"  // Kan läggas till
});
```

ProblemDetails är **industri-standard** och ger:

- Strukturerad felhantering
- Machine-readable format
- Dokumentations-länkar
- Consistent error format

### **7. Parallell exekvering för prestanda**

Senior använder `Task.WhenAll` för att matcha alla jobb parallellt:

```csharp
var matchTasks = jobs.Select(job => _matchingStrategy.CalculateMatchAsync(cvSkills, job));
var matches = await Task.WhenAll(matchTasks);  // ✅ Kör alla parallellt
```

Om det finns 50 jobb och varje matchning tar 10ms:

- **Sekventiell:** 50 \* 10ms = 500ms
- **Parallell:** ~10ms (limited by slowest)

Detta är **50x snabbare** för stora datamängder!

Junior kör sekventiellt i en loop - **mycket långsammare**.

### **8. Mängd kod vs kvalitet på abstraktioner**

| Metric             | Novis    | Junior   | Senior  |
| ------------------ | -------- | -------- | ------- |
| Antal filer        | 1        | 4        | 7       |
| Rader kod          | ~151     | ~200     | ~350+   |
| Antal interfaces   | 0        | 0        | 3       |
| Antal strategies   | 0        | 0        | 1+      |
| Matchning isolerad | ❌       | ❌       | ✅      |
| Nya algoritmer     | Omöjligt | Omöjligt | Trivial |
| Testbarhet         | Låg      | Låg      | Hög     |

Senior är **~2.3x mer kod**, men:

- **Obegränsat utökningsbar** med nya matchningsalgoritmer
- **Trivialt att A/B-testa** olika strategier
- **Fullständigt testbar** på varje nivå
- **Mycket bättre användarupplevelse** med missing skills och explanations

---

## Slutsats och rekommendationer

Match endpoint-analysen visar de **största skillnaderna** mellan nivåerna av alla endpoints:

1. **Strategy Pattern är kritiskt** för flexibla matchningsalgoritmer
2. **Rich domain models** (MissingSkills, MatchExplanation) ger stor användarnytta
3. **Server-side intelligence** vs client-side work är fundamental arkitekturfråga
4. **Parallell exekvering** kan ge 10-50x prestanda i real-world scenarios
5. **Sofistikerad algoritm** (frequency, penalty, bonus) vs enkel string matching

Detta är **starkast bevis** för er hypotes:

> **Promptarens förståelse för design patterns, arkitekturprinciper och användarupplevelse avgör om koden blir underhållbar och värdefull.**

Novis och Junior implementerar "matchning" men ger liten faktisk nytta. Senior implementerar en **produkt-ready feature** med fokus på användarupplevelse, prestanda och underhållbarhet.

using Senior.Models;

namespace Senior.Services;

/// <summary>
/// Application-lager interface för CV-analys
/// Följer Interface Segregation Principle och Dependency Inversion Principle
/// </summary>
public interface ICvAnalysisService
{
    /// <summary>
    /// Analyserar CV-text och extraherar kompetenser
    /// </summary>
    /// <param name="request">CV-analysförfrågan</param>
    /// <returns>Analysresultat med extraherade kompetenser</returns>
    Task<CvAnalysisResponse> AnalyzeCvAsync(CvAnalysisRequest request);
}

/// <summary>
/// Interface för kompetensextraktorer
/// Gör det möjligt att utöka med nya extraktorer utan att ändra befintlig kod (Open/Closed Principle)
/// </summary>
public interface ISkillExtractor
{
    /// <summary>
    /// Extraherar specifika kompetenser från CV-text
    /// </summary>
    List<string> Extract(string cvText, string? targetRole = null);
}

/// <summary>
/// Implementation av CV-analysservice
/// Använder Strategy Pattern för att göra kompetensextraktion utökningsbar
/// </summary>
public class CvAnalysisService : ICvAnalysisService
{
    private readonly ILogger<CvAnalysisService> _logger;
    private readonly IEnumerable<ISkillExtractor> _extractors;

    /// <summary>
    /// Constructor med dependency injection för logger och extractors
    /// </summary>
    public CvAnalysisService(
        ILogger<CvAnalysisService> logger,
        IEnumerable<ISkillExtractor> extractors)
    {
        _logger = logger;
        _extractors = extractors;
    }

    public async Task<CvAnalysisResponse> AnalyzeCvAsync(CvAnalysisRequest request)
    {
        _logger.LogInformation("Påbörjar CV-analys för text med längd: {Length} tecken", request.CvText.Length);

        if (string.IsNullOrWhiteSpace(request.CvText))
        {
            _logger.LogWarning("Tomt CV mottaget");
            throw new ArgumentException("CV-text kan inte vara tom", nameof(request));
        }

        try
        {
            // Simulera asynkron bearbetning (t.ex. för framtida ML-modell eller externt API)
            await Task.Delay(100);

            var response = new CvAnalysisResponse();

            // Kör alla registrerade extractors
            // Detta gör att vi kan lägga till nya extractors utan att ändra denna kod (Open/Closed)
            foreach (var extractor in _extractors)
            {
                var skills = extractor.Extract(request.CvText, request.TargetRole);

                // Kategorisera baserat på extractor-typ (kan förbättras med mer specifika interfaces)
                if (extractor is TechnicalSkillExtractor)
                {
                    response.TechnicalSkills.AddRange(skills);
                }
                else if (extractor is ProgrammingLanguageExtractor)
                {
                    response.ProgrammingLanguages.AddRange(skills);
                }
                else if (extractor is FrameworkExtractor)
                {
                    response.Frameworks.AddRange(skills);
                }
                else if (extractor is SoftSkillExtractor)
                {
                    response.SoftSkills.AddRange(skills);
                }
            }

            // Extrahera erfarenhet
            response.YearsOfExperience = ExtractYearsOfExperience(request.CvText);

            // Generera sammanfattning
            response.Summary = GenerateSummary(response);

            _logger.LogInformation(
                "CV-analys slutförd: {TechSkills} tekniska kompetenser, {ProgLangs} språk, {Frameworks} ramverk",
                response.TechnicalSkills.Count,
                response.ProgrammingLanguages.Count,
                response.Frameworks.Count
            );

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fel vid CV-analys");
            throw;
        }
    }

    /// <summary>
    /// Extraherar antal års erfarenhet från CV-text
    /// </summary>
    private int? ExtractYearsOfExperience(string cvText)
    {
        var patterns = new[]
        {
            @"(\d+)\+?\s*år?s?\s*(erfarenhet|experience)",
            @"(erfarenhet|experience).*?(\d+)\+?\s*år"
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                cvText,
                pattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            if (match.Success)
            {
                var yearGroup = match.Groups[1].Value;
                if (string.IsNullOrEmpty(yearGroup))
                    yearGroup = match.Groups[2].Value;

                if (int.TryParse(yearGroup, out int years))
                {
                    return years;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Genererar en sammanfattning av analysen
    /// </summary>
    private string GenerateSummary(CvAnalysisResponse response)
    {
        var totalSkills = response.TechnicalSkills.Count +
                         response.ProgrammingLanguages.Count +
                         response.Frameworks.Count +
                         response.SoftSkills.Count;

        var summary = $"CV-analys identifierade totalt {totalSkills} kompetenser";

        if (response.ProgrammingLanguages.Any())
        {
            summary += $", inklusive programmeringsspråk som {string.Join(", ", response.ProgrammingLanguages.Take(3))}";
        }

        if (response.YearsOfExperience.HasValue)
        {
            summary += $". Cirka {response.YearsOfExperience} års erfarenhet.";
        }
        else
        {
            summary += ".";
        }

        return summary;
    }
}

/// <summary>
/// Extraherar tekniska kompetenser
/// Kan enkelt utökas med fler keywords eller ersättas med ML-modell
/// </summary>
public class TechnicalSkillExtractor : ISkillExtractor
{
    private readonly HashSet<string> _technicalKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Docker", "Kubernetes", "CI/CD", "DevOps", "Microservices", "REST API",
        "GraphQL", "Git", "Agile", "Scrum", "Azure", "AWS", "GCP",
        "SQL", "NoSQL", "MongoDB", "PostgreSQL", "Redis",
        "Unit Testing", "Integration Testing", "TDD", "BDD"
    };

    public List<string> Extract(string cvText, string? targetRole = null)
    {
        return _technicalKeywords
            .Where(keyword => cvText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();
    }
}

/// <summary>
/// Extraherar programmeringsspråk
/// </summary>
public class ProgrammingLanguageExtractor : ISkillExtractor
{
    private readonly HashSet<string> _languages = new(StringComparer.OrdinalIgnoreCase)
    {
        "C#", "JavaScript", "TypeScript", "Python", "Java", "C++", "Go", "Rust",
        "PHP", "Ruby", "Swift", "Kotlin", "Scala", "F#", "Erlang", "Elixir"
    };

    public List<string> Extract(string cvText, string? targetRole = null)
    {
        return _languages
            .Where(lang => cvText.Contains(lang, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();
    }
}

/// <summary>
/// Extraherar ramverk och bibliotek
/// </summary>
public class FrameworkExtractor : ISkillExtractor
{
    private readonly HashSet<string> _frameworks = new(StringComparer.OrdinalIgnoreCase)
    {
        ".NET", "ASP.NET", "Entity Framework", "React", "Angular", "Vue",
        "Node.js", "Express", "Django", "Flask", "Spring Boot", "Laravel",
        "jQuery", "Bootstrap", "Tailwind", "Next.js", "Blazor"
    };

    public List<string> Extract(string cvText, string? targetRole = null)
    {
        return _frameworks
            .Where(fw => cvText.Contains(fw, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();
    }
}

/// <summary>
/// Extraherar mjuka kompetenser (soft skills)
/// </summary>
public class SoftSkillExtractor : ISkillExtractor
{
    private readonly HashSet<string> _softSkills = new(StringComparer.OrdinalIgnoreCase)
    {
        "Kommunikation", "Problemlösning", "Teamwork", "Ledarskap", "Analytisk",
        "Kreativ", "Självgående", "Ansvarstagande", "Flexibel", "Strukturerad",
        "Communication", "Problem-solving", "Leadership", "Analytical", "Creative"
    };

    public List<string> Extract(string cvText, string? targetRole = null)
    {
        return _softSkills
            .Where(skill => cvText.Contains(skill, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();
    }
}

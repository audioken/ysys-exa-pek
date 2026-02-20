using Junior.Models;

namespace Junior.Services;

public class CvService
{
    private readonly ILogger<CvService> _logger;

    // Lista med vanliga kompetenser att söka efter
    private readonly HashSet<string> _knownSkills = new(StringComparer.OrdinalIgnoreCase)
    {
        "C#", "Java", "Python", "JavaScript", "TypeScript", "SQL", "HTML", "CSS",
        "React", "Angular", "Vue", "Node.js", "ASP.NET", ".NET", "Spring",
        "Docker", "Kubernetes", "AWS", "Azure", "GCP",
        "Git", "CI/CD", "Agile", "Scrum", "DevOps",
        "REST", "API", "Microservices", "GraphQL",
        "MongoDB", "PostgreSQL", "MySQL", "Redis",
        "Leadership", "Communication", "Teamwork", "Problem Solving"
    };

    public CvService(ILogger<CvService> logger)
    {
        _logger = logger;
    }

    public Task<CvAnalyzeResponse> AnalyzeCvAsync(string cvText)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(cvText))
            {
                _logger.LogWarning("Empty CV text provided for analysis");
                return Task.FromResult(new CvAnalyzeResponse
                {
                    Skills = new List<string>(),
                    TotalSkillsFound = 0
                });
            }

            var identifiedSkills = new List<string>();

            // Analysera texten och hitta kompetenser
            foreach (var skill in _knownSkills)
            {
                if (cvText.Contains(skill, StringComparison.OrdinalIgnoreCase))
                {
                    identifiedSkills.Add(skill);
                }
            }

            _logger.LogInformation("Analyzed CV and found {SkillCount} skills", identifiedSkills.Count);

            var response = new CvAnalyzeResponse
            {
                Skills = identifiedSkills.OrderBy(s => s).ToList(),
                TotalSkillsFound = identifiedSkills.Count
            };

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while analyzing CV");
            return Task.FromResult(new CvAnalyzeResponse
            {
                Skills = new List<string>(),
                TotalSkillsFound = 0
            });
        }
    }
}

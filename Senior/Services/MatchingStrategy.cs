using Senior.Models;

namespace Senior.Services;

/// <summary>
/// Interface för matchningsstrategier
/// Följer Strategy Pattern och Open/Closed Principle - 
/// Nya matchningsstrategier kan läggas till utan att ändra befintlig kod
/// </summary>
public interface IMatchingStrategy
{
    /// <summary>
    /// Namn på strategin för identifiering
    /// </summary>
    string StrategyName { get; }

    /// <summary>
    /// Beräknar en matchningspoäng mellan CV-kompetenser och jobb
    /// </summary>
    /// <param name="cvSkills">Extraherade kompetenser från CV</param>
    /// <param name="job">Jobb att matcha mot</param>
    /// <returns>MatchResult med poäng och detaljer</returns>
    Task<MatchResult> CalculateMatchAsync(List<string> cvSkills, JobDto job);
}

/// <summary>
/// Kompetensbaserad matchningsstrategi
/// Matchar CV-kompetenser mot jobbannonser baserat på nyckelord och kompetenser
/// </summary>
public class SkillBasedMatchingStrategy : IMatchingStrategy
{
    private readonly ILogger<SkillBasedMatchingStrategy> _logger;

    public string StrategyName => "Skill-Based Matching";

    public SkillBasedMatchingStrategy(ILogger<SkillBasedMatchingStrategy> logger)
    {
        _logger = logger;
    }

    public Task<MatchResult> CalculateMatchAsync(List<string> cvSkills, JobDto job)
    {
        var result = new MatchResult
        {
            Job = job
        };

        // Kombinera headline och description för matchning
        var jobText = $"{job.Headline} {job.Description}".ToLowerInvariant();

        // Hitta matchade kompetenser
        var matchedSkills = new List<string>();
        var skillOccurrences = new Dictionary<string, int>();

        foreach (var skill in cvSkills)
        {
            var skillLower = skill.ToLowerInvariant();
            var count = CountOccurrences(jobText, skillLower);

            if (count > 0)
            {
                matchedSkills.Add(skill);
                skillOccurrences[skill] = count;
            }
        }

        result.MatchedSkills = matchedSkills;

        // Extrahera relevanta kompetenser från jobbannonsen som saknas i CV
        var missingSkills = ExtractMissingSkills(jobText, cvSkills);
        result.MissingSkills = missingSkills;

        // Beräkna matchningspoäng
        // Baspoäng: andel matchade kompetenser
        var baseScore = cvSkills.Count > 0
            ? (matchedSkills.Count * 100) / cvSkills.Count
            : 0;

        // Bonuspoäng för multipla förekomster av samma kompetens
        var frequencyBonus = skillOccurrences.Values.Sum(c => Math.Min(c - 1, 5));

        // Avdrag för saknade kompetenser
        var missingPenalty = Math.Min(missingSkills.Count * 5, 30);

        result.MatchScore = Math.Clamp(baseScore + frequencyBonus - missingPenalty, 0, 100);

        // Skapa förklaring
        result.MatchExplanation = GenerateExplanation(
            matchedSkills.Count,
            missingSkills.Count,
            cvSkills.Count,
            skillOccurrences);

        _logger.LogDebug("Matchning för jobb {JobId}: {Score} poäng", job.Id, result.MatchScore);

        return Task.FromResult(result);
    }

    private int CountOccurrences(string text, string keyword)
    {
        if (string.IsNullOrEmpty(keyword)) return 0;

        var count = 0;
        var index = 0;

        while ((index = text.IndexOf(keyword, index, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            count++;
            index += keyword.Length;
        }

        return count;
    }

    private List<string> ExtractMissingSkills(string jobText, List<string> cvSkills)
    {
        // Enkel implementation - i en verklig applikation skulle detta vara mer sofistikerat
        var commonRequiredSkills = new[]
        {
            "C#", "Java", "Python", "JavaScript", "TypeScript", "React", "Angular", "Vue",
            ".NET", "ASP.NET", "Node.js", "SQL", "NoSQL", "Docker", "Kubernetes",
            "Azure", "AWS", "Git", "CI/CD", "Agile", "Scrum", "REST", "API",
            "Microservices", "TDD", "Clean Code", "SOLID", "Design Patterns"
        };

        var cvSkillsLower = cvSkills.Select(s => s.ToLowerInvariant()).ToHashSet();
        var missing = new List<string>();

        foreach (var skill in commonRequiredSkills)
        {
            var skillLower = skill.ToLowerInvariant();

            // Om kompetensen finns i jobbet men inte i CV
            if (jobText.Contains(skillLower, StringComparison.OrdinalIgnoreCase)
                && !cvSkillsLower.Contains(skillLower))
            {
                missing.Add(skill);
            }
        }

        return missing;
    }

    private string GenerateExplanation(
        int matchedCount,
        int missingCount,
        int totalCvSkills,
        Dictionary<string, int> occurrences)
    {
        var matchPercentage = totalCvSkills > 0
            ? (matchedCount * 100) / totalCvSkills
            : 0;

        var explanation = $"Matchning: {matchedCount} av {totalCvSkills} kompetenser ({matchPercentage}%). ";

        if (matchedCount > 0)
        {
            var topSkills = occurrences
                .OrderByDescending(kv => kv.Value)
                .Take(3)
                .Select(kv => $"{kv.Key} ({kv.Value}x)");

            explanation += $"Starka matchningar: {string.Join(", ", topSkills)}. ";
        }

        if (missingCount > 0)
        {
            explanation += $"{missingCount} relevanta kompetenser saknas i CV:t.";
        }
        else if (matchedCount > 0)
        {
            explanation += "Inga kritiska kompetenser saknas.";
        }

        return explanation;
    }
}

/// <summary>
/// Exempel på ytterligare matchningsstrategi - Keywords-baserad matchning
/// Demonstrerar hur nya strategier enkelt kan läggas till (Open/Closed Principle)
/// </summary>
public class KeywordMatchingStrategy : IMatchingStrategy
{
    private readonly ILogger<KeywordMatchingStrategy> _logger;

    public string StrategyName => "Keyword Matching";

    public KeywordMatchingStrategy(ILogger<KeywordMatchingStrategy> logger)
    {
        _logger = logger;
    }

    public Task<MatchResult> CalculateMatchAsync(List<string> cvSkills, JobDto job)
    {
        var result = new MatchResult
        {
            Job = job
        };

        // Enkel keyword-baserad matchning
        var jobText = $"{job.Headline} {job.Description}".ToLowerInvariant();
        var cvText = string.Join(" ", cvSkills).ToLowerInvariant();

        // Räkna gemensamma ord (minst 3 tecken)
        var jobWords = jobText.Split(new[] { ' ', ',', '.', ';', ':', '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 3)
            .ToHashSet();

        var cvWords = cvText.Split(new[] { ' ', ',', '.', ';', ':', '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 3)
            .ToHashSet();

        var commonWords = jobWords.Intersect(cvWords).ToList();

        result.MatchedSkills = commonWords.Take(10).ToList();
        result.MatchScore = Math.Min((commonWords.Count * 100) / Math.Max(jobWords.Count, 1), 100);
        result.MatchExplanation = $"Keyword-matchning: {commonWords.Count} gemensamma nyckelord hittades.";

        _logger.LogDebug("Keyword-matchning för jobb {JobId}: {Score} poäng", job.Id, result.MatchScore);

        return Task.FromResult(result);
    }
}

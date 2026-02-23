using Senior.Models;

namespace Senior.Services;

/// <summary>
/// Application-lager interface för matchningsservice
/// Följer Interface Segregation Principle och Dependency Inversion Principle
/// </summary>
public interface IMatchingService
{
    /// <summary>
    /// Matchar CV mot relevanta jobb och returnerar matchningsresultat
    /// </summary>
    /// <param name="request">Matchningsförfrågan med CV och sökkriterier</param>
    /// <returns>Matchningsresultat med poäng och detaljer</returns>
    Task<MatchResponse> MatchCvWithJobsAsync(MatchRequest request);
}

/// <summary>
/// Implementation av matchningsservice
/// Följer Single Responsibility Principle - ansvarar endast för att koordinera matchningsprocessen
/// Följer Dependency Inversion Principle - beror på interfaces (ICvAnalysisService, IJobService, IMatchingStrategy)
/// Följer Open/Closed Principle - kan utökas med nya strategier utan att ändra denna klass
/// </summary>
public class MatchingService : IMatchingService
{
    private readonly ICvAnalysisService _cvAnalysisService;
    private readonly IJobService _jobService;
    private readonly IMatchingStrategy _matchingStrategy;
    private readonly ILogger<MatchingService> _logger;

    /// <summary>
    /// Constructor med dependency injection
    /// IMatchingStrategy injiceras automatiskt - enkelt att byta strategi i DI-konfiguration
    /// </summary>
    public MatchingService(
        ICvAnalysisService cvAnalysisService,
        IJobService jobService,
        IMatchingStrategy matchingStrategy,
        ILogger<MatchingService> logger)
    {
        _cvAnalysisService = cvAnalysisService;
        _jobService = jobService;
        _matchingStrategy = matchingStrategy;
        _logger = logger;
    }

    public async Task<MatchResponse> MatchCvWithJobsAsync(MatchRequest request)
    {
        _logger.LogInformation("Startar matchningsprocess");

        // Steg 1: Extrahera kompetenser från CV
        _logger.LogDebug("Extraherar kompetenser från CV");
        var cvAnalysisRequest = new CvAnalysisRequest
        {
            CvText = request.CvText,
            TargetRole = request.SearchQuery
        };

        var cvAnalysis = await _cvAnalysisService.AnalyzeCvAsync(cvAnalysisRequest);

        // Kombinera alla extraherade kompetenser från CV-analysen
        var cvSkills = new List<string>();
        cvSkills.AddRange(cvAnalysis.TechnicalSkills);
        cvSkills.AddRange(cvAnalysis.SoftSkills);
        cvSkills.AddRange(cvAnalysis.ProgrammingLanguages);
        cvSkills.AddRange(cvAnalysis.Frameworks);

        _logger.LogInformation("Extraherade {SkillCount} kompetenser från CV", cvSkills.Count);

        // Steg 2: Hämta relevanta jobb
        var jobs = await GetJobsToMatchAsync(request);

        _logger.LogInformation("Hittade {JobCount} jobb att utvärdera", jobs.Count);

        if (!jobs.Any())
        {
            return new MatchResponse
            {
                Matches = new List<MatchResult>(),
                TotalJobsEvaluated = 0,
                ExtractedSkills = cvSkills,
                MatchingStrategy = _matchingStrategy.StrategyName
            };
        }

        // Steg 3: Beräkna matchningar för varje jobb med vald strategi
        _logger.LogDebug("Använder matchningsstrategi: {Strategy}", _matchingStrategy.StrategyName);

        var matchTasks = jobs.Select(job => _matchingStrategy.CalculateMatchAsync(cvSkills, job));
        var matches = await Task.WhenAll(matchTasks);

        // Steg 4: Filtrera och sortera resultat
        var filteredMatches = matches
            .Where(m => m.MatchScore >= request.MinimumMatchScore)
            .OrderByDescending(m => m.MatchScore)
            .Take(request.MaxResults)
            .ToList();

        _logger.LogInformation(
            "Matchning klar: {MatchCount} matchningar över tröskelvärde {MinScore} av {TotalJobs} jobb",
            filteredMatches.Count,
            request.MinimumMatchScore,
            jobs.Count);

        return new MatchResponse
        {
            Matches = filteredMatches,
            TotalJobsEvaluated = jobs.Count,
            ExtractedSkills = cvSkills,
            MatchingStrategy = _matchingStrategy.StrategyName
        };
    }

    /// <summary>
    /// Hämtar jobb att matcha mot baserat på request-parametrar
    /// Följer Single Responsibility Principle - separerad metod för jobbhämtning
    /// </summary>
    private async Task<List<JobDto>> GetJobsToMatchAsync(MatchRequest request)
    {
        // Om specifika jobb-ID:n anges skulle vi kunna filtrera på dessa
        // I nuvarande implementation söker vi baserat på query och filtrerar sedan

        // Sök efter jobb baserat på sökkriterier
        var searchQuery = request.SearchQuery ?? "developer";
        _logger.LogDebug("Söker jobb med sökning: {Query}", searchQuery);

        // Hämta fler jobb för bättre matchning
        var limit = Math.Min(request.MaxResults * 3, 100);
        var searchResults = await _jobService.GetJobsAsync(searchQuery, limit);
        var jobs = searchResults.ToList();

        // Om specifika jobb-ID:n anges, filtrera på dessa
        if (request.JobIds != null && request.JobIds.Any())
        {
            _logger.LogDebug("Filtrerar på specifika jobb-ID:n");
            jobs = jobs.Where(j => request.JobIds.Contains(j.Id)).ToList();
        }

        return jobs;
    }
}

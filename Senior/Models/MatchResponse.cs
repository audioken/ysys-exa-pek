namespace Senior.Models;

/// <summary>
/// Response-modell för matchningsresultat
/// </summary>
public class MatchResponse
{
    /// <summary>
    /// Lista med matchningar sorterade efter matchningspoäng (högst först)
    /// </summary>
    public List<MatchResult> Matches { get; set; } = new();

    /// <summary>
    /// Totalt antal jobb som utvärderades
    /// </summary>
    public int TotalJobsEvaluated { get; set; }

    /// <summary>
    /// Kompetenser som extraherades från CV:t
    /// </summary>
    public List<string> ExtractedSkills { get; set; } = new();

    /// <summary>
    /// Matchningsmetod som användes
    /// </summary>
    public string MatchingStrategy { get; set; } = string.Empty;
}

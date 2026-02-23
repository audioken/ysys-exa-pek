namespace Senior.Models;

/// <summary>
/// Request-modell för matchning mellan CV och jobb
/// </summary>
public class MatchRequest
{
    /// <summary>
    /// CV-text som ska matchas
    /// </summary>
    public string CvText { get; set; } = string.Empty;

    /// <summary>
    /// Lista med jobb-ID:n att matcha mot (om specifika jobb önskas)
    /// </summary>
    public List<string>? JobIds { get; set; }

    /// <summary>
    /// Valfritt: Sökord för att hitta relevanta jobb att matcha mot
    /// </summary>
    public string? SearchQuery { get; set; }

    /// <summary>
    /// Valfritt: Platsfilter för jobb
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Valfritt: Maximal geografisk radie i km
    /// </summary>
    public int? RadiusKm { get; set; }

    /// <summary>
    /// Valfritt: Minsta matchningspoäng (0-100)
    /// </summary>
    public int MinimumMatchScore { get; set; } = 0;

    /// <summary>
    /// Valfritt: Maximalt antal matchningar att returnera
    /// </summary>
    public int MaxResults { get; set; } = 10;
}

namespace Senior.Models;

/// <summary>
/// Representerar en matchning mellan CV och ett specifikt jobb
/// </summary>
public class MatchResult
{
    /// <summary>
    /// Jobb-information
    /// </summary>
    public JobDto Job { get; set; } = new();

    /// <summary>
    /// Matchningspoäng (0-100)
    /// </summary>
    public int MatchScore { get; set; }

    /// <summary>
    /// Matchade kompetenser från CV som finns i jobbannonsen
    /// </summary>
    public List<string> MatchedSkills { get; set; } = new();

    /// <summary>
    /// Saknade kompetenser som efterfrågas i jobbet men saknas i CV
    /// </summary>
    public List<string> MissingSkills { get; set; } = new();

    /// <summary>
    /// Detaljerad förklaring av matchningen
    /// </summary>
    public string MatchExplanation { get; set; } = string.Empty;
}

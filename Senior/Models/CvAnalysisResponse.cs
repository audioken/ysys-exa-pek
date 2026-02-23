namespace Senior.Models;

/// <summary>
/// Response-modell för CV-analys
/// </summary>
public class CvAnalysisResponse
{
    /// <summary>
    /// Extraherade tekniska kompetenser
    /// </summary>
    public List<string> TechnicalSkills { get; set; } = new();

    /// <summary>
    /// Extraherade mjuka kompetenser (soft skills)
    /// </summary>
    public List<string> SoftSkills { get; set; } = new();

    /// <summary>
    /// Identifierade programmeringsspråk
    /// </summary>
    public List<string> ProgrammingLanguages { get; set; } = new();

    /// <summary>
    /// Identifierade ramverk och bibliotek
    /// </summary>
    public List<string> Frameworks { get; set; } = new();

    /// <summary>
    /// Antal års erfarenhet (om det går att extrahera)
    /// </summary>
    public int? YearsOfExperience { get; set; }

    /// <summary>
    /// Sammanfattning av analysen
    /// </summary>
    public string Summary { get; set; } = string.Empty;
}

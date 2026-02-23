namespace Senior.Models;

/// <summary>
/// Request-modell för CV-analys
/// </summary>
public class CvAnalysisRequest
{
    /// <summary>
    /// CV-text som ska analyseras
    /// </summary>
    public string CvText { get; set; } = string.Empty;

    /// <summary>
    /// Valfritt: Yrkesinriktning för mer specifik analys
    /// </summary>
    public string? TargetRole { get; set; }
}

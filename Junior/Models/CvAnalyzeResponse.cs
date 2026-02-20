namespace Junior.Models;

public class CvAnalyzeResponse
{
    public List<string> Skills { get; set; } = new();
    public int TotalSkillsFound { get; set; }
}

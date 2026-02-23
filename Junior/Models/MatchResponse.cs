namespace Junior.Models;

public class MatchResponse
{
    public List<JobMatch> Matches { get; set; } = new();
    public int TotalMatches { get; set; }
    public List<string> IdentifiedSkills { get; set; } = new();
}

public class JobMatch
{
    public string JobTitle { get; set; } = string.Empty;
    public string Employer { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> MatchedSkills { get; set; } = new();
    public double MatchScore { get; set; }
    public string JobUrl { get; set; } = string.Empty;
}

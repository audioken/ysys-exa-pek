namespace Senior.Models;

/// <summary>
/// Response från Arbetsförmedlingens API
/// </summary>
public class JobSearchResponse
{
    public int Total { get; set; }
    public List<JobSearchHit> Hits { get; set; } = new();
}

public class JobSearchHit
{
    public string Id { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public JobDescription? Description { get; set; }
    public JobEmployer? Employer { get; set; }
    public JobWorkplace? Workplace_address { get; set; }
    public DateTime Publication_date { get; set; }
    public string? Application_deadline { get; set; }
}

public class JobDescription
{
    public string? Text { get; set; }
}

public class JobEmployer
{
    public string? Name { get; set; }
}

public class JobWorkplace
{
    public string? Municipality { get; set; }
    public string? Region { get; set; }
}

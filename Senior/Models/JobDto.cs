namespace Senior.Models;

/// <summary>
/// Data Transfer Object för jobb som returneras till klienten
/// </summary>
public class JobDto
{
    public string Id { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Employer { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime PublicationDate { get; set; }
    public string ApplicationDeadline { get; set; } = string.Empty;
}

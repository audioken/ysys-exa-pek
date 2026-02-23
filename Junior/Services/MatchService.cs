using Junior.Models;
using System.Text.Json;

namespace Junior.Services;

public class MatchService
{
    private readonly CvService _cvService;
    private readonly JobsService _jobsService;
    private readonly ILogger<MatchService> _logger;

    public MatchService(CvService cvService, JobsService jobsService, ILogger<MatchService> logger)
    {
        _cvService = cvService;
        _jobsService = jobsService;
        _logger = logger;
    }

    public async Task<MatchResponse> MatchCvToJobsAsync(string cvText)
    {
        try
        {
            // Extrahera kompetenser från CV
            var cvAnalysis = await _cvService.AnalyzeCvAsync(cvText);
            var cvSkills = cvAnalysis.Skills;

            if (cvSkills.Count == 0)
            {
                _logger.LogWarning("No skills found in CV text");
                return new MatchResponse
                {
                    Matches = new List<JobMatch>(),
                    TotalMatches = 0,
                    IdentifiedSkills = new List<string>()
                };
            }

            _logger.LogInformation("Found {SkillCount} skills in CV", cvSkills.Count);

            // Hämta jobbannonser
            var jobsData = await _jobsService.GetJobsAsync();
            var jobs = ParseJobsFromResponse(jobsData);

            if (jobs.Count == 0)
            {
                _logger.LogWarning("No jobs found to match against");
                return new MatchResponse
                {
                    Matches = new List<JobMatch>(),
                    TotalMatches = 0,
                    IdentifiedSkills = cvSkills
                };
            }

            _logger.LogInformation("Matching CV skills against {JobCount} jobs", jobs.Count);

            // Matcha kompetenser mot jobbannonser
            var matches = new List<JobMatch>();

            foreach (var job in jobs)
            {
                var matchedSkills = new List<string>();
                var jobContent = $"{job.Title} {job.Description}".ToLower();

                // Hitta vilka kompetenser som matchar jobbannonsens beskrivning
                foreach (var skill in cvSkills)
                {
                    if (jobContent.Contains(skill.ToLower()))
                    {
                        matchedSkills.Add(skill);
                    }
                }

                // Beräkna matchningspoäng (procent av CV-kompetenser som matchar)
                if (matchedSkills.Count > 0)
                {
                    double matchScore = (double)matchedSkills.Count / cvSkills.Count * 100;

                    matches.Add(new JobMatch
                    {
                        JobTitle = job.Title,
                        Employer = job.Employer,
                        Description = TruncateDescription(job.Description),
                        MatchedSkills = matchedSkills,
                        MatchScore = Math.Round(matchScore, 2),
                        JobUrl = job.Url
                    });
                }
            }

            // Sortera matchningar efter poäng (högst först)
            var sortedMatches = matches.OrderByDescending(m => m.MatchScore).ToList();

            _logger.LogInformation("Found {MatchCount} job matches", sortedMatches.Count);

            return new MatchResponse
            {
                Matches = sortedMatches,
                TotalMatches = sortedMatches.Count,
                IdentifiedSkills = cvSkills
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while matching CV to jobs");
            return new MatchResponse
            {
                Matches = new List<JobMatch>(),
                TotalMatches = 0,
                IdentifiedSkills = new List<string>()
            };
        }
    }

    private List<JobData> ParseJobsFromResponse(object jobsData)
    {
        try
        {
            // Konvertera objektet tillbaka till JSON och sedan till en strukturerad lista
            var jsonString = JsonSerializer.Serialize(jobsData);
            var jsonDocument = JsonDocument.Parse(jsonString);

            var jobs = new List<JobData>();

            // API:et returnerar antingen en "hits" array eller direkt data
            if (jsonDocument.RootElement.TryGetProperty("hits", out var hitsElement))
            {
                foreach (var hit in hitsElement.EnumerateArray())
                {
                    jobs.Add(ExtractJobData(hit));
                }
            }

            return jobs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing jobs data");
            return new List<JobData>();
        }
    }

    private JobData ExtractJobData(JsonElement jobElement)
    {
        var job = new JobData();

        if (jobElement.TryGetProperty("headline", out var headline))
        {
            job.Title = headline.GetString() ?? "Okänd titel";
        }

        if (jobElement.TryGetProperty("employer", out var employer))
        {
            if (employer.TryGetProperty("name", out var employerName))
            {
                job.Employer = employerName.GetString() ?? "Okänd arbetsgivare";
            }
        }

        if (jobElement.TryGetProperty("description", out var description))
        {
            if (description.TryGetProperty("text", out var descText))
            {
                job.Description = descText.GetString() ?? "";
            }
            else
            {
                job.Description = description.GetString() ?? "";
            }
        }

        if (jobElement.TryGetProperty("webpage_url", out var url))
        {
            job.Url = url.GetString() ?? "";
        }

        return job;
    }

    private string TruncateDescription(string description, int maxLength = 200)
    {
        if (string.IsNullOrEmpty(description))
            return "";

        if (description.Length <= maxLength)
            return description;

        return description.Substring(0, maxLength) + "...";
    }

    private class JobData
    {
        public string Title { get; set; } = string.Empty;
        public string Employer { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}

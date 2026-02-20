using Microsoft.AspNetCore.Mvc;

namespace Novis.Controllers;

[ApiController]
[Route("[controller]")]
public class MatchController : ControllerBase
{
    public class JobData
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> RequiredSkills { get; set; } = new();
    }

    public class MatchRequest
    {
        public List<string> Competencies { get; set; } = new();
        public List<JobData> Jobs { get; set; } = new();
    }

    public class JobMatch
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> RequiredSkills { get; set; } = new();
        public List<string> MatchedSkills { get; set; } = new();
        public int MatchScore { get; set; }
        public double MatchPercentage { get; set; }
    }

    public class MatchResponse
    {
        public List<JobMatch> Matches { get; set; } = new();
        public int TotalJobs { get; set; }
        public int MatchedJobs { get; set; }
    }

    [HttpPost]
    public IActionResult Match([FromBody] MatchRequest request)
    {
        if (request?.Competencies == null || request.Competencies.Count == 0)
        {
            return BadRequest("Competencies are required");
        }

        if (request.Jobs == null || request.Jobs.Count == 0)
        {
            return BadRequest("Jobs list is required");
        }

        var matches = CalculateMatches(request.Competencies, request.Jobs);

        var response = new MatchResponse
        {
            Matches = matches,
            TotalJobs = request.Jobs.Count,
            MatchedJobs = matches.Count
        };

        return Ok(response);
    }

    private List<JobMatch> CalculateMatches(List<string> competencies, List<JobData> jobs)
    {
        var normalizedCompetencies = competencies
            .Select(c => c.Trim().ToLower())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .ToList();

        var jobMatches = new List<JobMatch>();

        foreach (var job in jobs)
        {
            var normalizedRequiredSkills = job.RequiredSkills
                .Select(s => s.Trim().ToLower())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct()
                .ToList();

            // Find matching skills
            var matchedSkills = normalizedCompetencies
                .Where(comp => normalizedRequiredSkills.Any(skill =>
                    skill.Contains(comp) || comp.Contains(skill) ||
                    AreSimilarSkills(comp, skill)))
                .ToList();

            // Calculate match score
            int matchScore = matchedSkills.Count;
            double matchPercentage = normalizedRequiredSkills.Count > 0
                ? (double)matchScore / normalizedRequiredSkills.Count * 100
                : 0;

            // Include jobs with at least one match
            if (matchScore > 0)
            {
                jobMatches.Add(new JobMatch
                {
                    Id = job.Id,
                    Title = job.Title,
                    Description = job.Description,
                    RequiredSkills = job.RequiredSkills,
                    MatchedSkills = matchedSkills,
                    MatchScore = matchScore,
                    MatchPercentage = Math.Round(matchPercentage, 2)
                });
            }
        }

        // Sort by match score (descending) and then by match percentage
        return jobMatches
            .OrderByDescending(m => m.MatchScore)
            .ThenByDescending(m => m.MatchPercentage)
            .ToList();
    }

    private bool AreSimilarSkills(string skill1, string skill2)
    {
        // Define common skill variations
        var skillMappings = new Dictionary<string, List<string>>
        {
            { "javascript", new List<string> { "js", "ecmascript" } },
            { "typescript", new List<string> { "ts" } },
            { "c#", new List<string> { "csharp", "c sharp" } },
            { ".net", new List<string> { "dotnet", "asp.net", "aspnet" } },
            { "python", new List<string> { "py" } },
            { "react", new List<string> { "reactjs", "react.js" } },
            { "vue", new List<string> { "vuejs", "vue.js" } },
            { "angular", new List<string> { "angularjs" } },
            { "node", new List<string> { "nodejs", "node.js" } },
            { "sql", new List<string> { "mysql", "postgresql", "mssql" } }
        };

        foreach (var mapping in skillMappings)
        {
            var allVariations = new List<string> { mapping.Key };
            allVariations.AddRange(mapping.Value);

            if (allVariations.Contains(skill1) && allVariations.Contains(skill2))
            {
                return true;
            }
        }

        return false;
    }
}

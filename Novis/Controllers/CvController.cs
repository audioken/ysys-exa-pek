using Microsoft.AspNetCore.Mvc;

namespace Novis.Controllers;

[ApiController]
[Route("[controller]")]
public class CvController : ControllerBase
{
    public class CvAnalyzeRequest
    {
        public string CvText { get; set; } = string.Empty;
    }

    public class CvAnalyzeResponse
    {
        public List<string> Competencies { get; set; } = new();
        public int TotalFound { get; set; }
    }

    [HttpPost("analyze")]
    public IActionResult Analyze([FromBody] CvAnalyzeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.CvText))
        {
            return BadRequest("CV text is required");
        }

        var competencies = ExtractCompetencies(request.CvText);

        var response = new CvAnalyzeResponse
        {
            Competencies = competencies,
            TotalFound = competencies.Count
        };

        return Ok(response);
    }

    private List<string> ExtractCompetencies(string cvText)
    {
        var foundCompetencies = new List<string>();
        var cvTextLower = cvText.ToLower();

        // Define common competencies to look for
        var competencyKeywords = new Dictionary<string, List<string>>
        {
            { "C#", new List<string> { "c#", "csharp", "c sharp" } },
            { ".NET", new List<string> { ".net", "dotnet", "asp.net", "aspnet" } },
            { "JavaScript", new List<string> { "javascript", "js", "ecmascript" } },
            { "TypeScript", new List<string> { "typescript", "ts" } },
            { "Python", new List<string> { "python" } },
            { "Java", new List<string> { "java" } },
            { "SQL", new List<string> { "sql", "mysql", "postgresql", "mssql", "oracle" } },
            { "React", new List<string> { "react", "reactjs", "react.js" } },
            { "Angular", new List<string> { "angular", "angularjs" } },
            { "Vue", new List<string> { "vue", "vuejs", "vue.js" } },
            { "Node.js", new List<string> { "node", "nodejs", "node.js" } },
            { "Docker", new List<string> { "docker", "containerization" } },
            { "Kubernetes", new List<string> { "kubernetes", "k8s" } },
            { "Azure", new List<string> { "azure", "microsoft azure" } },
            { "AWS", new List<string> { "aws", "amazon web services" } },
            { "Git", new List<string> { "git", "github", "gitlab", "version control" } },
            { "Agile", new List<string> { "agile", "scrum", "kanban" } },
            { "REST API", new List<string> { "rest", "restful", "api", "web api" } },
            { "Microservices", new List<string> { "microservices", "microservice" } },
            { "CI/CD", new List<string> { "ci/cd", "continuous integration", "continuous deployment" } },
            { "HTML", new List<string> { "html", "html5" } },
            { "CSS", new List<string> { "css", "css3", "scss", "sass" } },
            { "MongoDB", new List<string> { "mongodb", "mongo" } },
            { "Redis", new List<string> { "redis" } },
            { "GraphQL", new List<string> { "graphql" } },
            { "Machine Learning", new List<string> { "machine learning", "ml", "ai", "artificial intelligence" } },
            { "DevOps", new List<string> { "devops" } },
            { "Test-Driven Development", new List<string> { "tdd", "test-driven", "unit testing" } },
            { "Project Management", new List<string> { "project management", "projektledning" } },
            { "Leadership", new List<string> { "leadership", "ledarskap", "team lead" } },
            { "Communication", new List<string> { "communication", "kommunikation" } }
        };

        foreach (var competency in competencyKeywords)
        {
            foreach (var keyword in competency.Value)
            {
                if (cvTextLower.Contains(keyword))
                {
                    if (!foundCompetencies.Contains(competency.Key))
                    {
                        foundCompetencies.Add(competency.Key);
                    }
                    break;
                }
            }
        }

        return foundCompetencies.OrderBy(c => c).ToList();
    }
}

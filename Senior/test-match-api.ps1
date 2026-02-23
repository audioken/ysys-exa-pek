# Test-skript för POST /match endpoint
# Testar matchning mellan CV och jobb

$baseUrl = "http://localhost:5000"

Write-Host "=== Test av POST /match ===" -ForegroundColor Cyan
Write-Host ""

# Exempeldata: Ett CV för en .NET-utvecklare
$cvText = @"
Erfaren mjukvaruutvecklare med 5 års erfarenhet av .NET och C#.
Har arbetat med ASP.NET Core, Web API, Entity Framework och SQL Server.
Stark kompetens inom Clean Architecture, SOLID-principer och Design Patterns.
Erfarenhet av Docker, Kubernetes och Azure Cloud.
Arbetat i agila team med Scrum och CI/CD.
Har även kunskaper i React och TypeScript för frontend-utveckling.
"@

# Testfall 1: Grundläggande matchning
Write-Host "📝 Testfall 1: Grundläggande matchning med sökord 'developer'" -ForegroundColor Yellow
$request1 = @{
    cvText = $cvText
    searchQuery = "developer"
    maxResults = 5
    minimumMatchScore = 0
} | ConvertTo-Json

try {
    $response1 = Invoke-RestMethod -Uri "$baseUrl/match" -Method Post -Body $request1 -ContentType "application/json"
    
    Write-Host "✅ Matchning lyckades!" -ForegroundColor Green
    Write-Host "   Matchningsstrategi: $($response1.matchingStrategy)" -ForegroundColor Gray
    Write-Host "   Extraherade kompetenser: $($response1.extractedSkills.Count)" -ForegroundColor Gray
    Write-Host "   Utvärderade jobb: $($response1.totalJobsEvaluated)" -ForegroundColor Gray
    Write-Host "   Hittade matchningar: $($response1.matches.Count)" -ForegroundColor Gray
    Write-Host ""
    
    if ($response1.matches.Count -gt 0) {
        Write-Host "   Topp 3 matchningar:" -ForegroundColor White
        $response1.matches | Select-Object -First 3 | ForEach-Object {
            Write-Host "   - [$($_.matchScore)%] $($_.job.headline)" -ForegroundColor Cyan
            Write-Host "     Arbetsgivare: $($_.job.employer)" -ForegroundColor Gray
            Write-Host "     Matchade kompetenser: $($_.matchedSkills.Count)" -ForegroundColor Gray
            Write-Host "     Saknade kompetenser: $($_.missingSkills.Count)" -ForegroundColor Gray
            Write-Host ""
        }
    }
} catch {
    Write-Host "❌ Fel: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "---" -ForegroundColor Gray
Write-Host ""

# Testfall 2: Matchning med högre tröskelvärde
Write-Host "📝 Testfall 2: Matchning med minimumpoäng 50" -ForegroundColor Yellow
$request2 = @{
    cvText = $cvText
    searchQuery = ".NET developer"
    maxResults = 10
    minimumMatchScore = 50
} | ConvertTo-Json

try {
    $response2 = Invoke-RestMethod -Uri "$baseUrl/match" -Method Post -Body $request2 -ContentType "application/json"
    
    Write-Host "✅ Matchning lyckades!" -ForegroundColor Green
    Write-Host "   Utvärderade jobb: $($response2.totalJobsEvaluated)" -ForegroundColor Gray
    Write-Host "   Matchningar över 50%: $($response2.matches.Count)" -ForegroundColor Gray
    Write-Host ""
    
    if ($response2.matches.Count -gt 0) {
        $avgScore = ($response2.matches | Measure-Object -Property matchScore -Average).Average
        Write-Host "   Genomsnittlig matchningspoäng: $([math]::Round($avgScore, 1))%" -ForegroundColor White
    }
} catch {
    Write-Host "❌ Fel: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "---" -ForegroundColor Gray
Write-Host ""

# Testfall 3: Matchning med platsfilter (om API:et stödjer detta)
Write-Host "📝 Testfall 3: Matchning med platsfilter Stockholm" -ForegroundColor Yellow
$request3 = @{
    cvText = $cvText
    searchQuery = "C# developer"
    location = "Stockholm"
    maxResults = 5
    minimumMatchScore = 0
} | ConvertTo-Json

try {
    $response3 = Invoke-RestMethod -Uri "$baseUrl/match" -Method Post -Body $request3 -ContentType "application/json"
    
    Write-Host "✅ Matchning lyckades!" -ForegroundColor Green
    Write-Host "   Matchningar i Stockholm: $($response3.matches.Count)" -ForegroundColor Gray
    
    if ($response3.matches.Count -gt 0) {
        Write-Host ""
        Write-Host "   Platser:" -ForegroundColor White
        $response3.matches | Select-Object -First 5 | ForEach-Object {
            Write-Host "   - $($_.job.location)" -ForegroundColor Gray
        }
    }
} catch {
    Write-Host "❌ Fel: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "---" -ForegroundColor Gray
Write-Host ""

# Testfall 4: Valideringsfel - tom CV-text
Write-Host "📝 Testfall 4: Valideringstest (tom CV-text)" -ForegroundColor Yellow
$request4 = @{
    cvText = ""
    searchQuery = "developer"
} | ConvertTo-Json

try {
    $response4 = Invoke-RestMethod -Uri "$baseUrl/match" -Method Post -Body $request4 -ContentType "application/json"
    Write-Host "❌ Förväntat fel men fick OK-svar" -ForegroundColor Red
} catch {
    if ($_.Exception.Response.StatusCode -eq 400) {
        Write-Host "✅ Validering fungerar korrekt (400 Bad Request)" -ForegroundColor Green
    } else {
        Write-Host "❌ Oväntat fel: $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== Tester slutförda ===" -ForegroundColor Cyan

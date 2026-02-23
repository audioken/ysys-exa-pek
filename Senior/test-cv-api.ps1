# Test script för CV-analys endpoint

Write-Host "Testar CV-analys API..." -ForegroundColor Cyan

# Test 1: Analysera ett exempel-CV
Write-Host "`nTest 1: POST /cv/analyze (fullständigt CV)" -ForegroundColor Yellow
try {
    $cvRequest = @{
        cvText     = @"
Senior .NET-utvecklare med 8 års erfarenhet inom systemutveckling.
Expertis inom C#, ASP.NET Core, Entity Framework och Azure.
Erfarenhet av React, TypeScript och REST API-design.
Arbetat med Docker, Kubernetes och CI/CD-pipelines.
Stark problemlösningsförmåga och ledarskap inom Agile-team.
Databaskunskaper i SQL och MongoDB.
"@
        targetRole = "Backend Developer"
    }
    
    $json = $cvRequest | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "http://localhost:5086/cv/analyze" `
        -Method Post `
        -Body $json `
        -ContentType "application/json"
    
    Write-Host "✓ CV analyserat framgångsrikt" -ForegroundColor Green
    Write-Host "`n  Sammanfattning:" -ForegroundColor Cyan
    Write-Host "  $($response.summary)" -ForegroundColor Gray
    Write-Host "`n  Programmeringsspråk ($($response.programmingLanguages.Count)):" -ForegroundColor Cyan
    $response.programmingLanguages | ForEach-Object { Write-Host "    - $_" -ForegroundColor Gray }
    Write-Host "`n  Tekniska kompetenser ($($response.technicalSkills.Count)):" -ForegroundColor Cyan
    $response.technicalSkills | ForEach-Object { Write-Host "    - $_" -ForegroundColor Gray }
    Write-Host "`n  Ramverk ($($response.frameworks.Count)):" -ForegroundColor Cyan
    $response.frameworks | ForEach-Object { Write-Host "    - $_" -ForegroundColor Gray }
    Write-Host "`n  Mjuka kompetenser ($($response.softSkills.Count)):" -ForegroundColor Cyan
    $response.softSkills | ForEach-Object { Write-Host "    - $_" -ForegroundColor Gray }
    
    if ($response.yearsOfExperience) {
        Write-Host "`n  År av erfarenhet: $($response.yearsOfExperience)" -ForegroundColor Cyan
    }
}
catch {
    Write-Host "✗ Fel: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "  Detaljer: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}

# Test 2: Tom CV-text (ska ge valideringsfel)
Write-Host "`nTest 2: POST /cv/analyze (tom CV-text - förväntat fel)" -ForegroundColor Yellow
try {
    $emptyRequest = @{
        cvText = ""
    }
    
    $json = $emptyRequest | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "http://localhost:5086/cv/analyze" `
        -Method Post `
        -Body $json `
        -ContentType "application/json"
    
    Write-Host "✗ Förväntade valideringsfel, men fick svar" -ForegroundColor Red
}
catch {
    Write-Host "✓ Fick förväntat valideringsfel (400 Bad Request)" -ForegroundColor Green
}

# Test 3: Minimalt CV
Write-Host "`nTest 3: POST /cv/analyze (minimalt CV)" -ForegroundColor Yellow
try {
    $minimalRequest = @{
        cvText = "Python developer with Django experience. Strong communication skills."
    }
    
    $json = $minimalRequest | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "http://localhost:5086/cv/analyze" `
        -Method Post `
        -Body $json `
        -ContentType "application/json"
    
    Write-Host "✓ Minimalt CV analyserat" -ForegroundColor Green
    Write-Host "  Språk: $($response.programmingLanguages -join ', ')" -ForegroundColor Gray
    Write-Host "  Ramverk: $($response.frameworks -join ', ')" -ForegroundColor Gray
    Write-Host "  Soft skills: $($response.softSkills -join ', ')" -ForegroundColor Gray
}
catch {
    Write-Host "✗ Fel: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Testning slutförd!" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

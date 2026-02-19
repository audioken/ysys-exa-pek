# Test script för Senior Jobs API

Write-Host "Testar Senior Jobs API..." -ForegroundColor Cyan

# Test 1: Hämta jobb utan filter
Write-Host "`nTest 1: GET /jobs (standard)" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5086/jobs" -Method Get
    Write-Host "✓ Hämtade $($response.Count) jobb" -ForegroundColor Green
    if ($response.Count -gt 0) {
        Write-Host "  Första jobbet: $($response[0].headline)" -ForegroundColor Gray
    }
} catch {
    Write-Host "✗ Fel: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Sök efter "utvecklare"
Write-Host "`nTest 2: GET /jobs?q=utvecklare&limit=5" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5086/jobs?q=utvecklare&limit=5" -Method Get
    Write-Host "✓ Hämtade $($response.Count) jobb med 'utvecklare'" -ForegroundColor Green
    if ($response.Count -gt 0) {
        Write-Host "  Första jobbet: $($response[0].headline)" -ForegroundColor Gray
        Write-Host "  Arbetsgivare: $($response[0].employer)" -ForegroundColor Gray
        Write-Host "  Plats: $($response[0].location)" -ForegroundColor Gray
    }
} catch {
    Write-Host "✗ Fel: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Health endpoint (från tidigare)
Write-Host "`nTest 3: GET /health" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5086/health" -Method Get
    Write-Host "✓ Health check: $($response.status)" -ForegroundColor Green
} catch {
    Write-Host "✗ Fel: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nTester klara!" -ForegroundColor Cyan

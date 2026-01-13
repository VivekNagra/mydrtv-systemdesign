# Demo script for repeatable e2e run
$ErrorActionPreference = 'Stop'

# Config
$composeFile = 'infra/docker-compose.yml'
$IDENTITY  = 'http://localhost:8081'
$CATALOGUE = 'http://localhost:8082'
$RATINGS   = 'http://localhost:8083'
$SEARCH    = 'http://localhost:8084'
$RECS      = 'http://localhost:8085'

Write-Host '--- docker compose up --build ---'
docker compose -f $composeFile up -d --build

function Wait-Healthy($url, $name) {
  $max = 40
  for ($i=0; $i -lt $max; $i++) {
    try {
      $resp = Invoke-RestMethod -Method Get -Uri $url -TimeoutSec 2
      if ($resp.status -eq 'ok') { Write-Host "[$name] healthy"; return }
    } catch { Start-Sleep 1 }
  }
  throw "Timed out waiting for $name health"
}

Write-Host '--- health checks ---'
Wait-Healthy "$IDENTITY/health" 'identity'
Wait-Healthy "$CATALOGUE/health" 'catalogue'
Wait-Healthy "$RATINGS/health" 'ratings'
Wait-Healthy "$SEARCH/health" 'search'
Wait-Healthy "$RECS/health" 'recs'

Write-Host '--- seed catalogue ---'
Invoke-RestMethod -Method Post -Uri "$CATALOGUE/admin/seed" -ContentType 'application/json' -Body '{}' | Out-Host

Write-Host '--- register/login ---'
$regBody = @{ email='demo@test.com'; password='Password123!'; displayName='Demo' } | ConvertTo-Json
try { Invoke-RestMethod -Method Post -Uri "$IDENTITY/auth/register" -ContentType 'application/json' -Body $regBody | Out-Null } catch {}
$loginBody = @{ email='demo@test.com'; password='Password123!' } | ConvertTo-Json
$resp = Invoke-RestMethod -Method Post -Uri "$IDENTITY/auth/login" -ContentType 'application/json' -Body $loginBody
$TOKEN = $resp.token
$AUTH  = @{ Authorization = "Bearer $TOKEN" }

Write-Host '--- pick first programme ---'
$programmes = Invoke-RestMethod -Method Get -Uri "$CATALOGUE/programmes"
$programmeId = $programmes[0].id

Write-Host '--- submit rating + summary ---'
$ratingBody = @{ programmeId = $programmeId; stars = 4; review = 'Good.' } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "$RATINGS/ratings" -Headers $AUTH -ContentType 'application/json' -Body $ratingBody | Out-Host
Invoke-RestMethod -Method Get  -Uri "$RATINGS/programmes/$programmeId/ratings/summary" | Out-Host

Write-Host '--- search ---'
Invoke-RestMethod -Method Get -Uri "$SEARCH/search?query=borgen" | Out-Host

Write-Host '--- recommendations ---'
Invoke-RestMethod -Method Get -Uri "$RECS/recommendations?genre=Drama&minRating=3" | Out-Host

Write-Host 'Demo complete.'

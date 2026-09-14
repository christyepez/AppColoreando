param(
  [string]$ProjectName = "appcoloreando-$env:COMPUTERNAME",
  [string]$EnvFile = ".env.hub"
)
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
Set-Location $repo
if (-not (Test-Path $EnvFile)) {
  throw "Missing $EnvFile. Copy .env.hub.example and configure local secrets."
}
$required = 'POSTGRES_PASSWORD','JWT_KEY','RABBITMQ_PASSWORD','MINIO_ROOT_PASSWORD'
$values = @{}
Get-Content $EnvFile | ForEach-Object {
  if ($_ -match '^([^#=]+)=(.*)$') {
    $values[$matches[1].Trim()] = $matches[2].Trim()
  }
}
foreach ($key in $required) {
  if (-not $values.ContainsKey($key) -or [string]::IsNullOrWhiteSpace($values[$key]) -or $values[$key] -match '^replace-with') {
    throw "Required secret $key is not configured in $EnvFile"
  }
}Write-Host "Pulling AppColoreando images for project $ProjectName..."
docker compose --env-file $EnvFile -p $ProjectName -f docker-compose.hub.yml pull
if ($LASTEXITCODE -ne 0) { throw 'Docker Hub pull failed. Verify docker login and repository access.' }
docker compose --env-file $EnvFile -p $ProjectName -f docker-compose.hub.yml up -d
if ($LASTEXITCODE -ne 0) { throw 'Docker compose up failed.' }
$apiPort = if ($values['API_PORT']) { $values['API_PORT'] } else { '8080' }
$processorPort = if ($values['VISUAL_PROCESSOR_PORT']) { $values['VISUAL_PROCESSOR_PORT'] } else { '8090' }
$adminPort = if ($values['ADMIN_PORT']) { $values['ADMIN_PORT'] } else { '4200' }
$webPort = if ($values['WEB_PORT']) { $values['WEB_PORT'] } else { '8083' }
$checks = @("http://localhost:$apiPort/health", "http://localhost:$processorPort/health")
foreach ($url in $checks) {
  $ok = $false
  for ($i = 0; $i -lt 30; $i++) {
    try {
      $response = Invoke-WebRequest -UseBasicParsing -TimeoutSec 3 $url
      if ($response.StatusCode -eq 200) { $ok = $true; break }
    } catch { Start-Sleep -Seconds 2 }
  }
  if (-not $ok) { throw "Health check failed: $url" }
  Write-Host "$url OK"
}
Write-Host "AppColoreando ready: Admin http://localhost:$adminPort | Web http://localhost:$webPort | API http://localhost:$apiPort"
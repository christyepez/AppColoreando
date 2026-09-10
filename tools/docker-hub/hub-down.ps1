param(
  [string]$ProjectName = "appcoloreando-$env:COMPUTERNAME",
  [string]$EnvFile = ".env.hub"
)
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
Set-Location $repo
if (-not (Test-Path $EnvFile)) {
  throw "Missing $EnvFile."
}
docker compose --env-file $EnvFile -p $ProjectName -f docker-compose.hub.yml down
if ($LASTEXITCODE -ne 0) { throw 'Docker compose down failed.' }
Write-Host "Stopped $ProjectName without deleting volumes."
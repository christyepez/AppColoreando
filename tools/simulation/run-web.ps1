param(
  [string]$ApiBaseUrl = 'http://localhost:8080',
  [int]$Port = 8083
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$mobile = Join-Path $repo 'apps\mobile'
Set-Location $mobile
flutter build web --release --dart-define="API_BASE_URL=$ApiBaseUrl"
flutter run -d web-server --web-port $Port --dart-define="API_BASE_URL=$ApiBaseUrl"

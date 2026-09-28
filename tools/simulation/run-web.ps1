param(
  [string]$ApiBaseUrl = 'http://127.0.0.1:8086',
  [int]$Port = 4210
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$mobile = Join-Path $repo 'apps\mobile'
Set-Location $mobile
flutter build web --release --dart-define="API_BASE_URL=$ApiBaseUrl"
flutter run -d web-server --web-port $Port --dart-define="API_BASE_URL=$ApiBaseUrl"

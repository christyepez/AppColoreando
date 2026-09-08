param(
  [ValidateSet('web','windows','android')]
  [string]$Target = 'web',
  [string]$ApiBaseUrl = 'http://localhost:8080',
  [string]$AndroidAvd = 'medium_phone'
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$mobile = Join-Path $repo 'apps\mobile'
Set-Location $mobile

switch ($Target) {
  'web' {
    flutter run -d chrome --dart-define="API_BASE_URL=$ApiBaseUrl"
  }
  'windows' {
    Set-Item -Path 'Env:ProgramFiles(x86)' -Value 'C:\Program Files (x86)'
    flutter run -d windows --dart-define="API_BASE_URL=$ApiBaseUrl"
  }
  'android' {
    $sdk = Join-Path $env:LOCALAPPDATA 'Android\Sdk'
    $adb = Join-Path $sdk 'platform-tools\adb.exe'
    $androidCli = Get-ChildItem "$env:LOCALAPPDATA\Microsoft\WinGet\Packages" -Recurse -Filter android.exe |
      Select-Object -First 1 -ExpandProperty FullName
    if (-not (Test-Path $adb)) { throw 'Android SDK/ADB is not installed.' }
    if (-not $androidCli) { throw 'Android CLI is not installed.' }

    $device = (& $adb devices) | Select-String 'emulator-\d+\s+device'
    if (-not $device) {
      & $androidCli --sdk $sdk emulator start $AndroidAvd
    }

    & $adb wait-for-device
    $serial = ((& $adb devices) | Select-String 'emulator-\d+\s+device' | Select-Object -First 1).ToString().Split("`t")[0]
    if (-not $serial) { throw 'Android emulator did not become ready.' }

    flutter run -d $serial --dart-define='API_BASE_URL=http://10.0.2.2:8080'
  }
}

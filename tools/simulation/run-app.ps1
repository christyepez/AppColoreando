param(
  [ValidateSet('web','windows','android')]
  [string]$Target = 'web',
  [string]$ApiBaseUrl = 'http://localhost:8080',
  [string]$AndroidAvd = 'appcoloreando_api35'
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
    $emulator = Join-Path $sdk 'emulator\emulator.exe'
    if (-not (Test-Path $adb)) { throw 'Android SDK/ADB is not installed.' }
    if (-not (Test-Path $emulator)) { throw 'Android Emulator is not installed.' }
    $device = (& $adb devices) | Select-String 'emulator-\d+\s+device'
    if (-not $device) {
      Start-Process -FilePath $emulator -ArgumentList "-avd $AndroidAvd -gpu host -no-snapshot-load -no-boot-anim"
    }

    $serial = $null
    for ($i = 0; $i -lt 90; $i++) {
      $match = ((& $adb devices) | Select-String 'emulator-\d+\s+device' | Select-Object -First 1)
      if ($match) {
        $serial = $match.ToString().Split("`t")[0]
        $boot = (& $adb -s $serial shell getprop sys.boot_completed 2>$null).Trim()
        if ($boot -eq '1') { break }
      }
      Start-Sleep -Seconds 2
    }

    if (-not $serial) { throw 'Android emulator did not become ready.' }
    $sdkLevel = (& $adb -s $serial shell getprop ro.build.version.sdk).Trim()
    Write-Host "Using $serial (Android API $sdkLevel)"
    flutter run -d $serial --dart-define='API_BASE_URL=http://10.0.2.2:8080'
  }
}

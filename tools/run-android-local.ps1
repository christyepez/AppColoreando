param(
    [string]$Device = "medium_phone",
    [string]$ApiBaseUrl = "http://10.0.2.2:8086"
)

$ErrorActionPreference = "Stop"
$repo = Split-Path $PSScriptRoot -Parent
$mobile = Join-Path $repo "apps\mobile"
$sdk = Join-Path $env:LOCALAPPDATA "Android\Sdk"
$preferredFlutter = Join-Path $env:USERPROFILE ".puro\envs\stable\flutter\bin\flutter.bat"
$flutter = if (Test-Path $preferredFlutter) { $preferredFlutter } else { "flutter" }

$studioJbr = "C:\Program Files\Android\Android Studio\jbr"
$javaHome = if (Test-Path (Join-Path $studioJbr "bin\java.exe")) {
    $studioJbr
} elseif ($env:JAVA_HOME -and (Test-Path (Join-Path $env:JAVA_HOME "bin\java.exe"))) {
    $env:JAVA_HOME
} else {
    throw "No Java runtime found. Install Android Studio or configure JAVA_HOME."
}

$androidCli = Get-ChildItem (Join-Path $env:LOCALAPPDATA "Microsoft\WinGet\Packages") -Recurse -Filter android.exe -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match "Google\.AndroidCLI" } |
    Select-Object -First 1 -ExpandProperty FullName
if (-not $androidCli) { throw "Android CLI not found. Install Google.AndroidCLI." }

$adb = Join-Path $sdk "platform-tools\adb.exe"
if (-not (Test-Path $adb)) { throw "ADB not found under $sdk." }

$env:ANDROID_SDK_ROOT = $sdk
$env:ANDROID_HOME = $sdk
$env:JAVA_HOME = $javaHome
$env:Path = "$javaHome\bin;$sdk\platform-tools;$sdk\emulator;$env:Path"

& $flutter config --android-sdk $sdk | Out-Null

$connected = & $adb devices
if ($connected -notmatch "emulator-\d+\s+device") {
    Start-Process -FilePath $androidCli -ArgumentList @("emulator", "start", $Device)
    & $adb wait-for-device
}

for ($i = 0; $i -lt 120; $i++) {
    $boot = (& $adb shell getprop sys.boot_completed 2>$null).Trim()
    if ($boot -eq "1") { break }
    Start-Sleep -Seconds 1
}

$emulatorId = (& $adb devices | Select-String "emulator-\d+\s+device" | Select-Object -First 1).ToString().Split()[0]
if (-not $emulatorId) { throw "Android emulator did not become available." }

Set-Location $mobile
& $flutter run -d $emulatorId "--dart-define=API_BASE_URL=$ApiBaseUrl"

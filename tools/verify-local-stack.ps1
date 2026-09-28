param(
    [string]$RepoRoot = (Split-Path $PSScriptRoot -Parent),
    [switch]$SkipBuilds,
    [switch]$RunBenchmarks
)

$ErrorActionPreference = 'Stop'
$failures = [System.Collections.Generic.List[string]]::new()

function Check-Http {
    param([string]$Name, [string]$Url, [int]$Expected = 200)
    try {
        $response = Invoke-WebRequest -UseBasicParsing $Url -TimeoutSec 10
        if ([int]$response.StatusCode -ne $Expected) {
            $failures.Add("$Name expected HTTP $Expected but got $($response.StatusCode)")
        } else {
            Write-Host "[PASS] $Name -> $Url ($($response.StatusCode))"
        }
    } catch {
        $failures.Add("$Name unreachable at $Url : $($_.Exception.Message)")
    }
}

Set-Location $RepoRoot

Write-Host "== AppColoreando S51 Local Production Preflight =="

$compose = docker compose config -q 2>&1
if ($LASTEXITCODE -ne 0) {
    $failures.Add("docker compose config invalid: $compose")
} else {
    Write-Host "[PASS] docker compose config"
}

Check-Http 'API health' 'http://127.0.0.1:8086/health'
Check-Http 'User Web' 'http://127.0.0.1:4210/'
Check-Http 'Admin' 'http://127.0.0.1:4211/'
Check-Http 'Visual Processor' 'http://127.0.0.1:8090/health'

try {
    $categories = Invoke-RestMethod 'http://127.0.0.1:8086/api/catalog/categories' -TimeoutSec 10
    $requiredSlugs = @('andean-landscapes','space-opera','comic-heroes','anime-adventure')
    foreach ($slug in $requiredSlugs) {
        if (-not ($categories | Where-Object slug -eq $slug)) {
            $failures.Add("Missing seeded category: $slug")
        }
    }
    if (-not $failures.Where({ $_ -like 'Missing seeded category*' }).Count) {
        Write-Host "[PASS] themed catalog seeds"
    }
} catch {
    $failures.Add("Catalog seed check failed: $($_.Exception.Message)")
}

try {
    $engine = Invoke-RestMethod 'http://127.0.0.1:8090/health' -TimeoutSec 10
    if ($engine.engine -notlike 's42-v2-*') {
        $failures.Add("Unexpected visual engine: $($engine.engine)")
    } else {
        Write-Host "[PASS] visual engine $($engine.engine)"
    }
} catch {
    $failures.Add("Visual engine metadata check failed: $($_.Exception.Message)")
}

$ports = @{
    8086 = 'AppColoreando API'
    4210 = 'AppColoreando Web'
    4211 = 'AppColoreando Admin'
    8090 = 'Visual Processor'
}
foreach ($entry in $ports.GetEnumerator()) {
    $listener = Get-NetTCPConnection -LocalPort $entry.Key -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $listener) {
        $failures.Add("$($entry.Value) is not listening on $($entry.Key)")
    } else {
        Write-Host "[PASS] $($entry.Value) listening on $($entry.Key)"
    }
}

if (-not $SkipBuilds) {
    Write-Host "Running backend Release build/tests..."
    dotnet build backend\AppColoreando.slnx -c Release --warnaserror --nologo
    if ($LASTEXITCODE -ne 0) { $failures.Add('Backend Release build failed') }

    dotnet test backend\AppColoreando.slnx -c Release --no-build --nologo
    if ($LASTEXITCODE -ne 0) { $failures.Add('Backend tests failed') }

    $flutter = Join-Path $env:USERPROFILE '.puro\envs\stable\flutter\bin\flutter.bat'
    if (-not (Test-Path $flutter)) { $flutter = 'flutter' }
    Push-Location apps\mobile
    & $flutter analyze
    if ($LASTEXITCODE -ne 0) { $failures.Add('Flutter analyze failed') }
    & $flutter test --exclude-tags benchmark
    if ($LASTEXITCODE -ne 0) { $failures.Add('Flutter functional tests failed') }
    if ($RunBenchmarks) {
        & $flutter test --tags benchmark
        if ($LASTEXITCODE -ne 0) { $failures.Add('Flutter benchmark tests failed') }
    }
    Pop-Location

    docker compose run --rm --no-deps visual-processor pytest -q
    if ($LASTEXITCODE -ne 0) { $failures.Add('Visual Processor tests failed') }
}

if ($failures.Count -gt 0) {
    Write-Host ""
    Write-Host "PRECHECK FAILED ($($failures.Count))" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host " - $_" -ForegroundColor Red }
    exit 1
}

Write-Host ""
Write-Host "PRECHECK PASSED" -ForegroundColor Green
exit 0

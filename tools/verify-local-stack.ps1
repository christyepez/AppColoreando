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

Write-Host "== AppColoreando Local Production Preflight =="

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

$starterArtworks = @(
    @{ Id = 'a1111111-1111-4111-8111-111111111111'; Title = 'Andean Summit & Lagoon' },
    @{ Id = 'a2222222-2222-4222-8222-222222222222'; Title = 'Twin Suns Patrol' },
    @{ Id = 'a3333333-3333-4333-8333-333333333333'; Title = 'Urban Guardian' },
    @{ Id = 'a4444444-4444-4444-8444-444444444444'; Title = 'Skyline Adventurer' },
    @{ Id = 'a5555555-5555-4555-8555-555555555555'; Title = 'Neon Sentinel Mecha' }
)
foreach ($starter in $starterArtworks) {
    try {
        $meta = Invoke-RestMethod "http://127.0.0.1:8086/api/catalog/artworks/$($starter.Id)" -TimeoutSec 10
        if ($meta.title -ne $starter.Title) {
            $failures.Add("Starter artwork title mismatch for $($starter.Id)")
            continue
        }
        if ([int]$meta.regionCount -lt 1) {
            $failures.Add("Starter artwork has no playable regions: $($starter.Title)")
            continue
        }
        $bundle = Invoke-WebRequest -UseBasicParsing "http://127.0.0.1:8086/api/catalog/artworks/$($starter.Id)/bundle" -TimeoutSec 10
        $thumb = Invoke-WebRequest -UseBasicParsing "http://127.0.0.1:8086/api/catalog/artworks/$($starter.Id)/thumbnail" -TimeoutSec 10
        if ([int]$bundle.StatusCode -ne 200 -or $bundle.Content.Length -lt 1000) {
            $failures.Add("Starter bundle invalid: $($starter.Title)")
            continue
        }
        if ([int]$thumb.StatusCode -ne 200 -or $thumb.RawContentLength -lt 1000) {
            $failures.Add("Starter thumbnail invalid: $($starter.Title)")
            continue
        }
        Write-Host "[PASS] starter $($starter.Title) ($($meta.regionCount) regions)"
    } catch {
        $failures.Add("Starter artwork check failed for $($starter.Title): $($_.Exception.Message)")
    }
}

try {
    $collections = Invoke-RestMethod 'http://127.0.0.1:8086/api/catalog/collections' -TimeoutSec 10
    $requiredCollections = @(
        'andes-ecuador',
        'space-opera-originals',
        'comic-originals',
        'anime-originals',
        'mecha-originals'
    )
    foreach ($slug in $requiredCollections) {
        $collection = $collections | Where-Object slug -eq $slug | Select-Object -First 1
        if ($null -eq $collection -or [int]$collection.artworkCount -lt 1) {
            $failures.Add("Starter collection missing or empty: $slug")
        }
    }
    if (-not $failures.Where({ $_ -like 'Starter collection*' }).Count) {
        Write-Host "[PASS] themed starter collections contain API artworks"
    }
} catch {
    $failures.Add("Starter collection check failed: $($_.Exception.Message)")
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

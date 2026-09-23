# Runs test suite with code coverage and displays a clean terminal table

Write-Host "Running tests with code coverage..." -ForegroundColor Cyan
dotnet test tests/YamlCLI.Tests/ /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura --verbosity quiet

$xmlFile = Get-ChildItem -Path "tests/YamlCLI.Tests" -Filter "coverage.cobertura.xml" -Recurse | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if (-not $xmlFile) {
    Write-Host "No coverage.cobertura.xml generated." -ForegroundColor Red
    exit 1
}

[xml]$cov = Get-Content $xmlFile.FullName

$map = [ordered]@{}

foreach ($cls in $cov.coverage.packages.package.classes.class) {
    $cleanName = ($cls.name -split '<')[0].TrimEnd('/')
    $cleanName = $cleanName -replace '^YamlCLI\.', ''
    if (-not $map.Contains($cleanName)) {
        $map[$cleanName] = @{ Total = 0; Covered = 0 }
    }
    $lines = @($cls.lines.line)
    $map[$cleanName].Total += $lines.Count
    $map[$cleanName].Covered += @($lines | Where-Object { [int]$_.hits -gt 0 }).Count
}

Write-Host ""
Write-Host ("=" * 75) -ForegroundColor DarkGray
Write-Host ("{0,-35} | {1,12} | {2,10} | {3,12}" -f "Class / Component", "Total Lines", "Covered", "Coverage %") -ForegroundColor White
Write-Host ("-" * 75) -ForegroundColor DarkGray

foreach ($key in ($map.Keys | Sort-Object)) {
    $t = $map[$key].Total
    $c = $map[$key].Covered
    $p = if ($t -gt 0) { [math]::Round(($c / $t) * 100, 1) } else { 0 }
    
    $color = if ($p -ge 85) { "Green" } elseif ($p -ge 70) { "Yellow" } else { "Red" }
    
    Write-Host ("{0,-35} | {1,12} | {2,10} | " -f $key, $t, $c) -NoNewline
    Write-Host ("{0,11}%" -f $p) -ForegroundColor $color
}

Write-Host ("=" * 75) -ForegroundColor DarkGray
$overallLines = [int]$cov.coverage.'lines-valid'
$overallCovered = [int]$cov.coverage.'lines-covered'
$lineRate = [math]::Round([double]$cov.coverage.'line-rate' * 100, 1)
$branchRate = [math]::Round([double]$cov.coverage.'branch-rate' * 100, 1)

Write-Host ("OVERALL LINE COVERAGE   : {0}% ({1}/{2} lines)" -f $lineRate, $overallCovered, $overallLines) -ForegroundColor Green
Write-Host ("OVERALL BRANCH COVERAGE : {0}%" -f $branchRate) -ForegroundColor Cyan
Write-Host ("=" * 75) -ForegroundColor DarkGray
Write-Host ""

# Script to run .NET 11 Scaffolding E2E Tests
# Usage: .\run-net11-e2e-tests.ps1 [-TestName <specific test name>] [-Category <category>]

param(
    [Parameter(Mandatory=$false)]
    [string]$TestName,
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("All", "Controller", "MinimalApi", "Blazor", "RazorPages", "View", "Identity", "Area", "Aspire")]
    [string]$Category = "All",
    
    [Parameter(Mandatory=$false)]
    [switch]$Verbose
)

# Set environment variable to enable skippable tests
$env:SCAFFOLDING_RunSkippableTests = "true"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host ".NET 11 Scaffolding E2E Test Runner" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get the script directory and navigate to repo root
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $scriptDir))
Set-Location $repoRoot

Write-Host "Repository Root: $repoRoot" -ForegroundColor Gray
Write-Host ""

# Build the test filter
$filter = "FullyQualifiedName~Net11ScaffoldingE2ETests"

if ($TestName) {
    $filter += "&FullyQualifiedName~$TestName"
    Write-Host "Running specific test: $TestName" -ForegroundColor Yellow
}
elseif ($Category -ne "All") {
    $filter += "&FullyQualifiedName~$Category"
    Write-Host "Running tests in category: $Category" -ForegroundColor Yellow
}
else {
    Write-Host "Running all .NET 11 E2E tests" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Filter: $filter" -ForegroundColor Gray
Write-Host ""

# Prepare dotnet test arguments
$testArgs = @(
    "test",
    "test\Scaffolding\E2E_Test\E2E_Test.Tests.csproj",
    "--filter", $filter
)

if ($Verbose) {
    $testArgs += @("--verbosity", "detailed")
}
else {
    $testArgs += @("--verbosity", "normal")
}

# Run the tests
Write-Host "Executing: dotnet $($testArgs -join ' ')" -ForegroundColor Gray
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$result = & dotnet $testArgs

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Tests completed successfully!" -ForegroundColor Green
}
else {
    Write-Host "❌ Tests failed with exit code: $LASTEXITCODE" -ForegroundColor Red
}

Write-Host "========================================" -ForegroundColor Cyan

exit $LASTEXITCODE

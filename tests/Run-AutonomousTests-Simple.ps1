#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Simplified autonomous test runner

.DESCRIPTION
    Runs E2E tests with TestFeed backsystem, finds feedback, and generates summary report.

.PARAMETER TestCategory
    The test category to run. Default is "Autonomous".

.PARAMETER ServerUrl
    URL of the running BlazorBook server. Default is http://localhost:5555

.PARAMETER StartServer
    If specified, will start the BlazorBook.Web server before running tests.
#>

param(
    [string]$TestCategory = "Autonomous",
    [string]$ServerUrl = "http://localhost:5555",
    [switch]$StartServer
)

$ErrorActionPreference = "Stop"

function Write-Header {
    param([string]$Message)
    Write-Host "`n$('=' * 80)" -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor White
    Write-Host "$('=' * 80)" -ForegroundColor Cyan
}

function Write-Success { param([string]$msg) Write-Host "Success: $msg" -ForegroundColor Green }
function Write-Info { param([string]$msg) Write-Host "Info: $msg" -ForegroundColor Blue }
function Write-Warning2 { param([string]$msg) Write-Host "Warning: $msg" -ForegroundColor Yellow }

Write-Header "🤖 AUTONOMOUS TEST RUNNER"

$rootDir = Split-Path -Parent $PSScriptRoot
$testProjectDir = Join-Path $PSScriptRoot "BlazorBook.E2E"
$feedbackBaseDir = Join-Path $testProjectDir "test-feedback"
$webProjectDir = Join-Path $rootDir "src\BlazorBook.Web"

# Start server if requested
$serverProcess = $null
if ($StartServer) {
    Write-Header "🚀 Starting Server"
    Push-Location $webProjectDir
    try {
        Write-Info "Building..."
        dotnet build --configuration Release | Out-Null
        
        Write-Info "Starting server..."
        $serverProcess = Start-Process -FilePath "dotnet" `
            -ArgumentList "run --no-build --configuration Release --urls $ServerUrl" `
            -WorkingDirectory $webProjectDir `
            -PassThru `
            -WindowStyle Hidden
        
        Start-Sleep -Seconds 10
        Write-Success "Server started"
    } finally {
        Pop-Location
    }
}

# Run tests
Write-Header "🧪 Running Tests"
Push-Location $testProjectDir
try {
    $testFilter = if ($TestCategory) { "--filter Category=$TestCategory" } else { "" }
    
    $testOutput = & dotnet test $testFilter --logger "console;verbosity=normal" 2>&1 | Out-String
    $testExitCode = $LASTEXITCODE
    
    if ($testOutput -match "Passed:\s+(\d+)") { $passedCount = [int]$matches[1] } else { $passedCount = 0 }
    if ($testOutput -match "Failed:\s+(\d+)") { $failedCount = [int]$matches[1] } else { $failedCount = 0 }
    if ($testOutput -match "Total:\s+(\d+)") { $totalCount = [int]$matches[1] } else { $totalCount = 0 }
    
    Write-Info "Results: $passedCount passed, $failedCount failed"
    
    if ($failedCount -eq 0) {
        Write-Success "All tests passed!"
    } else {
        Write-Warning2 "$failedCount tests failed"
    }
} finally {
    Pop-Location
}

# Analyze feedback
Write-Header "📊 Analyzing Feedback"

$feedbackDirs = Get-ChildItem -Path $feedbackBaseDir -Directory -ErrorAction SilentlyContinue | 
                Sort-Object LastWriteTime -Descending |
                Select-Object -First 10

$totalIssues = 0
$highIssues = 0
$medIssues = 0

if ($feedbackDirs) {
    Write-Info "Found $($feedbackDirs.Count) feedback reports"
    
    foreach ($dir in $feedbackDirs) {
        $reportJsonPath = Join-Path $dir.FullName "report.json"
        
        if (Test-Path $reportJsonPath) {
            $report = Get-Content $reportJsonPath | ConvertFrom-Json
            
            Write-Info "Report: $($report.TestName)"
            Write-Info "  UI Issues: $($report.UIIssues.Count)"
            
            $totalIssues += $report.UIIssues.Count
            $high = ($report.UIIssues | Where-Object { $_.Severity -eq "high" }).Count
            $med = ($report.UIIssues | Where-Object { $_.Severity -eq "medium" }).Count
            
            $highIssues += $high
            $medIssues += $med
            
            if ($high -gt 0) {
                Write-Warning2 "  High priority issues: $high"
            }
        }
    }
} else {
    Write-Warning2 "No feedback reports found"
}

# Summary
Write-Header "📈 SUMMARY"

Write-Info "Tests: $passedCount passed, $failedCount failed"
Write-Info "UI Issues Found: $totalIssues"
if ($highIssues -gt 0) {
    Write-Warning2 "  High Priority: $highIssues"
}
if ($medIssues -gt 0) {
    Write-Info "  Medium Priority: $medIssues"
}

# Save simple report
$reportDir = Join-Path $testProjectDir "autonomous-reports"
if (-not (Test-Path $reportDir)) {
    New-Item -ItemType Directory -Path $reportDir | Out-Null
}

$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$reportPath = Join-Path $reportDir "report-$timestamp.txt"

$lines = @()
$lines += "AUTONOMOUS TEST REPORT"
$lines += "Generated: $(Get-Date)"
$lines += "Category: $TestCategory"
$lines += ""
$lines += "SUMMARY:"
$lines += "  Tests: $passedCount passed, $failedCount failed"
$lines += "  UI Issues: $totalIssues total"
$lines += "  High Priority: $highIssues"
$lines += "  Medium Priority: $medIssues"
$lines += ""
$lines += "FEEDBACK LOCATION:"
$lines += "  $feedbackBaseDir"
$lines += ""
$lines += "STATUS:"

if ($failedCount -eq 0 -and $highIssues -eq 0) {
    $lines += "  ALL SYSTEMS GREEN - Demo Ready!"
} elseif ($failedCount -gt 0) {
    $lines += "  FUNCTIONAL ISSUES - Fix failing tests"
} elseif ($highIssues -gt 0) {
    $lines += "  HIGH PRIORITY ISSUES - Address before demo"
} else {
    $lines += "  TESTS PASSED - Minor UI issues to address"
}

$reportText = $lines -join "`n"
$reportText | Out-File -FilePath $reportPath -Encoding UTF8
Write-Success "Report saved: $reportPath"
Start-Process notepad.exe $reportPath

# Cleanup
if ($serverProcess) {
    Write-Header "🧹 Cleanup"
    Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
    Write-Success "Server stopped"
}

Write-Header "🎉 COMPLETE"
Write-Info "Feedback directory: $feedbackBaseDir"

exit $(if ($failedCount -gt 0) { 1 } else { 0 })

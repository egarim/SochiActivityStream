<#
.SYNOPSIS
    Verifies that the E2E testing environment is properly configured.

.DESCRIPTION
    Checks all prerequisites and configurations needed to run BlazorBook E2E tests.
    Provides helpful diagnostic information and setup instructions if issues are found.

.EXAMPLE
    .\Verify-TestEnvironment.ps1
#>

$ErrorActionPreference = "Stop"

function Write-StatusCheck {
    param([string]$Name, [bool]$Passed, [string]$Details = "")
    
    if ($Passed) {
        Write-Host "✓ " -ForegroundColor Green -NoNewline
        Write-Host "$Name" -ForegroundColor White
        if ($Details) {
            Write-Host "  $Details" -ForegroundColor Gray
        }
    }
    else {
        Write-Host "✗ " -ForegroundColor Red -NoNewline
        Write-Host "$Name" -ForegroundColor White
        if ($Details) {
            Write-Host "  $Details" -ForegroundColor Yellow
        }
    }
}

function Test-CommandExists {
    param([string]$Command)
    
    try {
        Get-Command $Command -ErrorAction Stop | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

Write-Host @"
╔═══════════════════════════════════════════════════════════╗
║                                                           ║
║     BlazorBook E2E Testing Environment Verification      ║
║                                                           ║
╚═══════════════════════════════════════════════════════════╝

"@ -ForegroundColor Cyan

$allPassed = $true

# Check .NET SDK
Write-Host "`n[1] Checking .NET SDK..." -ForegroundColor Yellow
$dotnetExists = Test-CommandExists "dotnet"
Write-StatusCheck ".NET SDK installed" $dotnetExists

if ($dotnetExists) {
    $dotnetVersion = dotnet --version
    $is8OrHigher = $dotnetVersion -match "^8\." -or $dotnetVersion -match "^9\."
    Write-StatusCheck ".NET 8.0+ available" $is8OrHigher "Version: $dotnetVersion"
    $allPassed = $allPassed -and $is8OrHigher
}
else {
    $allPassed = $false
    Write-Host "  Install from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
}

# Check PowerShell
Write-Host "`n[2] Checking PowerShell..." -ForegroundColor Yellow
$psVersion = $PSVersionTable.PSVersion
$psVersionOk = $psVersion.Major -ge 5
Write-StatusCheck "PowerShell installed" $psVersionOk "Version: $($psVersion.ToString())"
$allPassed = $allPassed -and $psVersionOk

# Check test project
Write-Host "`n[3] Checking Test Project..." -ForegroundColor Yellow
$testProjectDir = Join-Path $PSScriptRoot "BlazorBook.E2E"
$testProjectExists = Test-Path (Join-Path $testProjectDir "BlazorBook.E2E.csproj")
Write-StatusCheck "Test project found" $testProjectExists $testProjectDir
$allPassed = $allPassed -and $testProjectExists

if ($testProjectExists) {
    Push-Location $testProjectDir
    try {
        # Check if project builds
        Write-Host "  Building test project..." -ForegroundColor Gray
        $buildOutput = dotnet build --no-restore 2>&1
        $buildSuccess = $LASTEXITCODE -eq 0
        Write-StatusCheck "Test project builds" $buildSuccess
        $allPassed = $allPassed -and $buildSuccess
        
        if (!$buildSuccess) {
            Write-Host "  Build errors detected. Run 'dotnet build' for details." -ForegroundColor Yellow
        }
    }
    finally {
        Pop-Location
    }
}

# Check Playwright
Write-Host "`n[4] Checking Playwright..." -ForegroundColor Yellow
if ($testProjectExists) {
    $playwrightDll = Join-Path $testProjectDir "bin\Debug\net8.0\Microsoft.Playwright.dll"
    $playwrightExists = Test-Path $playwrightDll
    Write-StatusCheck "Playwright package installed" $playwrightExists
    
    if ($playwrightExists) {
        # Check if browsers are installed
        $browsersPath = Join-Path $env:LOCALAPPDATA "ms-playwright"
        $browsersInstalled = Test-Path $browsersPath
        Write-StatusCheck "Playwright browsers installed" $browsersInstalled $browsersPath
        
        if (!$browsersInstalled) {
            Write-Host "`n  To install browsers, run:" -ForegroundColor Yellow
            Write-Host "  cd tests/BlazorBook.E2E" -ForegroundColor Cyan
            Write-Host "  pwsh bin/Debug/net8.0/playwright.ps1 install" -ForegroundColor Cyan
            $allPassed = $false
        }
    }
    else {
        Write-Host "  Run 'dotnet build' in the test project to restore Playwright" -ForegroundColor Yellow
        $allPassed = $false
    }
}

# Check web project
Write-Host "`n[5] Checking Web Project..." -ForegroundColor Yellow
$scriptRoot = Split-Path -Parent $PSScriptRoot
$webProjectDir = Join-Path $scriptRoot "src\BlazorBook.Web"
$webProjectExists = Test-Path (Join-Path $webProjectDir "BlazorBook.Web.csproj")
Write-StatusCheck "BlazorBook.Web project found" $webProjectExists $webProjectDir
$allPassed = $allPassed -and $webProjectExists

# Check if server is running
Write-Host "`n[6] Checking Test Server..." -ForegroundColor Yellow
$defaultUrl = "http://localhost:5555"
try {
    $response = Invoke-WebRequest -Uri $defaultUrl -Method Head -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
    $serverRunning = $true
    Write-StatusCheck "BlazorBook.Web is running" $true $defaultUrl
}
catch {
    $serverRunning = $false
    Write-StatusCheck "BlazorBook.Web is running" $false "Not running on $defaultUrl"
    Write-Host "  Start with: cd src/BlazorBook.Web; dotnet run --urls $defaultUrl" -ForegroundColor Gray
}

# Check test files
Write-Host "`n[7] Checking Test Files..." -ForegroundColor Yellow
$testFiles = @(
    "Tests\ComprehensivePageTests.cs",
    "Tests\FeedTests.cs",
    "Tests\AuthenticationTests.cs",
    "Infrastructure\BlazorBookPageTest.cs",
    "Infrastructure\VisualTestHelper.cs"
)

$testFilesExist = $true
foreach ($testFile in $testFiles) {
    $fullPath = Join-Path $testProjectDir $testFile
    $exists = Test-Path $fullPath
    if (!$exists) {
        $testFilesExist = $false
    }
}

Write-StatusCheck "Test files present" $testFilesExist "$($testFiles.Count) test files checked"
$allPassed = $allPassed -and $testFilesExist

# Check scripts
Write-Host "`n[8] Checking Test Scripts..." -ForegroundColor Yellow
$scriptsExist = (Test-Path (Join-Path $PSScriptRoot "Run-ComprehensiveTests.ps1")) -and
                (Test-Path (Join-Path $PSScriptRoot "Generate-TestReport.ps1"))
Write-StatusCheck "Test orchestration scripts present" $scriptsExist
$allPassed = $allPassed -and $scriptsExist

# Summary
Write-Host "`n" -NoNewline
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan

if ($allPassed) {
    Write-Host "`n✓ All checks passed! You are ready to run tests." -ForegroundColor Green
    Write-Host "`nRun tests with:" -ForegroundColor White
    Write-Host "  .\Run-ComprehensiveTests.ps1" -ForegroundColor Cyan
    
    if (!$serverRunning) {
        Write-Host "`nOr start server automatically:" -ForegroundColor White
        Write-Host "  .\Run-ComprehensiveTests.ps1 -StartServer" -ForegroundColor Cyan
    }
}
else {
    Write-Host "`n⚠ Some issues were found. Please address them before running tests." -ForegroundColor Yellow
    Write-Host "`nQuick fix commands:" -ForegroundColor White
    
    if (!$dotnetExists) {
        Write-Host "  # Install .NET SDK from https://dotnet.microsoft.com/download" -ForegroundColor Cyan
    }
    
    if ($testProjectExists -and !$buildSuccess) {
        Write-Host "  cd tests/BlazorBook.E2E" -ForegroundColor Cyan
        Write-Host "  dotnet build" -ForegroundColor Cyan
    }
    
    if ($testProjectExists -and !$browsersInstalled) {
        Write-Host "  cd tests/BlazorBook.E2E" -ForegroundColor Cyan
        Write-Host "  pwsh bin/Debug/net8.0/playwright.ps1 install" -ForegroundColor Cyan
    }
}

Write-Host "`n📖 Documentation: tests/README-TESTING.md" -ForegroundColor Gray
Write-Host "📋 Quick Reference: tests/TESTING-CHEATSHEET.md" -ForegroundColor Gray
Write-Host ""

if ($allPassed) {
    exit 0
} else {
    exit 1
}

<#
.SYNOPSIS
    Runs comprehensive E2E tests for BlazorBook.Web application.

.DESCRIPTION
    This script orchestrates the complete testing flow:
    1. Optionally starts the BlazorBook.Web application
    2. Runs all E2E tests including visual tests
    3. Generates comprehensive HTML reports
    4. Opens the report in your browser

.PARAMETER StartServer
    If set, automatically starts the BlazorBook.Web server before running tests.
    Default: false (assumes server is already running)

.PARAMETER ServerUrl
    The URL where BlazorBook.Web is running.
    Default: http://localhost:5555

.PARAMETER Category
    Run only tests with the specified category (e.g., "UI", "Functionality", "Navigation").
    Default: runs all tests

.PARAMETER GenerateReport
    Generate an HTML test report after execution.
    Default: true

.PARAMETER OpenReport
    Automatically open the test report in the default browser.
    Default: true

.PARAMETER HeadlessMode
    Run tests in headless mode (no browser UI).
    Default: true

.PARAMETER ParallelWorkers
    Number of parallel test workers. Set to 1 for sequential execution.
    Default: 4

.PARAMETER VideoCapture
    Capture video recordings of test execution.
    Default: false (only on failure)

.EXAMPLE
    .\Run-ComprehensiveTests.ps1
    Runs all tests against an already-running server.

.EXAMPLE
    .\Run-ComprehensiveTests.ps1 -StartServer
    Starts the server and runs all tests.

.EXAMPLE
    .\Run-ComprehensiveTests.ps1 -Category "UI"
    Runs only UI tests.

.EXAMPLE
    .\Run-ComprehensiveTests.ps1 -HeadlessMode:$false
    Runs tests with visible browser windows (useful for debugging).
#>

param(
    [switch]$StartServer = $false,
    [string]$ServerUrl = "http://localhost:5555",
    [string]$Category = "",
    [bool]$GenerateReport = $true,
    [bool]$OpenReport = $true,
    [bool]$HeadlessMode = $true,
    [int]$ParallelWorkers = 4,
    [string]$VideoCapture = "retain-on-failure"
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent (Split-Path -Parent $ScriptDir)
$TestProjectDir = Join-Path $ScriptDir "BlazorBook.E2E"
$WebProjectDir = Join-Path $RootDir "src\BlazorBook.Web"
$ReportsDir = Join-Path $TestProjectDir "TestResults"
$ScreenshotsDir = Join-Path $TestProjectDir "screenshots"
$Timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"

# Color output helpers
function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

function Write-Step {
    param([string]$Message)
    Write-ColorOutput "`n========================================" "Cyan"
    Write-ColorOutput " $Message" "Cyan"
    Write-ColorOutput "========================================`n" "Cyan"
}

function Write-Success {
    param([string]$Message)
    Write-ColorOutput "✓ $Message" "Green"
}

function Write-Error-Message {
    param([string]$Message)
    Write-ColorOutput "✗ $Message" "Red"
}

function Write-Info {
    param([string]$Message)
    Write-ColorOutput "ℹ $Message" "Yellow"
}

# Main execution
try {
    Write-ColorOutput @"
╔═══════════════════════════════════════════════════════════╗
║                                                           ║
║         BlazorBook Comprehensive Test Runner             ║
║                                                           ║
╚═══════════════════════════════════════════════════════════╝
"@ "Magenta"

    Write-Info "Test Execution Time: $Timestamp"
    Write-Info "Root Directory: $RootDir"
    Write-Info "Test Project: $TestProjectDir"
    Write-Info ""

    # Step 1: Clean previous test results
    Write-Step "Cleaning Previous Test Results"
    
    if (Test-Path $ReportsDir) {
        Remove-Item -Path $ReportsDir -Recurse -Force
        Write-Success "Removed old test results"
    }

    if (Test-Path $ScreenshotsDir) {
        Remove-Item -Path $ScreenshotsDir -Recurse -Force
        Write-Success "Removed old screenshots"
    }

    New-Item -ItemType Directory -Path $ReportsDir -Force | Out-Null
    New-Item -ItemType Directory -Path $ScreenshotsDir -Force | Out-Null

    # Step 2: Start server if requested
    $ServerProcess = $null
    if ($StartServer) {
        Write-Step "Starting BlazorBook.Web Server"
        
        $ServerProcess = Start-Process -FilePath "dotnet" `
            -ArgumentList "run --project `"$WebProjectDir`" --urls $ServerUrl" `
            -PassThru `
            -NoNewWindow

        Write-Info "Waiting for server to start..."
        Start-Sleep -Seconds 5

        # Check if server is accessible
        try {
            $response = Invoke-WebRequest -Uri $ServerUrl -Method Get -UseBasicParsing -TimeoutSec 10
            Write-Success "Server is running at $ServerUrl"
        }
        catch {
            Write-Error-Message "Failed to connect to server at $ServerUrl"
            throw
        }
    }
    else {
        Write-Step "Using Existing Server"
        Write-Info "Server URL: $ServerUrl"
        Write-Info "Make sure BlazorBook.Web is running before proceeding!"
        
        # Verify server is accessible
        try {
            $response = Invoke-WebRequest -Uri $ServerUrl -Method Get -UseBasicParsing -TimeoutSec 5
            Write-Success "Server is accessible"
        }
        catch {
            Write-Error-Message "Cannot connect to server at $ServerUrl"
            Write-Info "Start the server with: dotnet run --project src/BlazorBook.Web"
            throw
        }
    }

    # Step 3: Install Playwright browsers if needed
    Write-Step "Ensuring Playwright Browsers Are Installed"
    
    Push-Location $TestProjectDir
    try {
        $playwrightPath = dotnet build --no-restore 2>&1 | Select-String "bin"
        pwsh "$TestProjectDir\bin\Debug\net8.0\playwright.ps1" install chromium 2>&1 | Out-Null
        Write-Success "Playwright browsers are ready"
    }
    catch {
        Write-Info "Playwright browsers may need installation (will be handled automatically)"
    }
    finally {
        Pop-Location
    }

    # Step 4: Build test arguments
    Write-Step "Running E2E Tests"
    
    $testArgs = @(
        "test",
        "`"$TestProjectDir`"",
        "--logger", "`"trx;LogFileName=test-results-$Timestamp.trx`"",
        "--logger", "`"console;verbosity=normal`"",
        "--results-directory", "`"$ReportsDir`""
    )

    # Add category filter if specified
    if ($Category) {
        $testArgs += @("--filter", "TestCategory=$Category")
        Write-Info "Running tests with category: $Category"
    }
    else {
        Write-Info "Running all test categories"
    }

    # Set environment variables
    $env:BLAZORBOOK_URL = $ServerUrl
    $env:PLAYWRIGHT_BROWSERS_PATH = Join-Path $env:LOCALAPPDATA "ms-playwright"
    
    if ($HeadlessMode) {
        $env:HEADED = "0"
        Write-Info "Running in headless mode"
    }
    else {
        $env:HEADED = "1"
        Write-Info "Running with visible browser"
    }

    # Run tests
    Write-Info "Executing tests..."
    Write-Info "Parallel Workers: $ParallelWorkers"
    Write-Info ""

    $testStartTime = Get-Date
    
    $testProcess = Start-Process -FilePath "dotnet" `
        -ArgumentList $testArgs `
        -NoNewWindow `
        -Wait `
        -PassThru

    $testEndTime = Get-Date
    $testDuration = $testEndTime - $testStartTime

    if ($testProcess.ExitCode -eq 0) {
        Write-Success "All tests passed!"
    }
    else {
        Write-Error-Message "Some tests failed (Exit Code: $($testProcess.ExitCode))"
    }

    Write-Info "Test Duration: $($testDuration.ToString('mm\:ss'))"

    # Step 5: Count screenshots
    Write-Step "Test Artifacts Summary"
    
    if (Test-Path $ScreenshotsDir) {
        $screenshotCount = (Get-ChildItem -Path $ScreenshotsDir -Filter "*.png" -Recurse).Count
        Write-Info "Screenshots captured: $screenshotCount"
        Write-Info "Screenshots location: $ScreenshotsDir"
    }

    # Step 6: Generate HTML report
    if ($GenerateReport) {
        Write-Step "Generating HTML Test Report"
        
        $reportScript = Join-Path $ScriptDir "Generate-TestReport.ps1"
        if (Test-Path $reportScript) {
            & $reportScript -ResultsDir $ReportsDir -ScreenshotsDir $ScreenshotsDir -Timestamp $Timestamp
            
            $htmlReport = Join-Path $ReportsDir "test-report-$Timestamp.html"
            if (Test-Path $htmlReport) {
                Write-Success "Report generated: $htmlReport"
                
                if ($OpenReport) {
                    Write-Info "Opening report in browser..."
                    Start-Process $htmlReport
                }
            }
        }
        else {
            Write-Info "Report generator script not found, skipping HTML report"
        }
    }

    # Step 7: Display summary
    Write-Step "Test Execution Complete"
    
    Write-ColorOutput @"

Summary:
--------
Test Duration:     $($testDuration.ToString('mm\:ss'))
Exit Code:         $($testProcess.ExitCode)
Results Directory: $ReportsDir
Screenshots:       $ScreenshotsDir

"@ "Cyan"

    if ($testProcess.ExitCode -eq 0) {
        Write-Success "✓ All tests completed successfully!"
        exit 0
    }
    else {
        Write-Error-Message "✗ Some tests failed. Check the reports for details."
        exit $testProcess.ExitCode
    }
}
catch {
    Write-Error-Message "An error occurred during test execution:"
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    exit 1
}
finally {
    # Cleanup: Stop server if we started it
    if ($ServerProcess -and !$ServerProcess.HasExited) {
        Write-Step "Cleaning Up"
        Write-Info "Stopping BlazorBook.Web server..."
        Stop-Process -Id $ServerProcess.Id -Force
        Write-Success "Server stopped"
    }

    # Reset environment variables
    Remove-Item Env:\BLAZORBOOK_URL -ErrorAction SilentlyContinue
    Remove-Item Env:\HEADED -ErrorAction SilentlyContinue
}

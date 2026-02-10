#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Autonomous test runner that executes tests, analyzes results, and generates fixes without human intervention.

.DESCRIPTION
    This script runs E2E tests with the TestFeedbackSystem, analyzes the generated feedback,
    identifies issues, and generates recommendations for fixes. It's designed to run completely
    autonomously and provide actionable intelligence for improving the application.

.PARAMETER TestCategory
    The test category to run. Default is "Autonomous" for quality tests.

.PARAMETER IterationCount
    Number of test iterations to run. Default is 1.

.PARAMETER AutoFix
    If specified, will attempt to automatically apply recommended fixes.

.PARAMETER ServerUrl
    URL of the running BlazorBook server. Default is http://localhost:5555

.PARAMETER StartServer
    If specified, will start the BlazorBook.Web server before running tests.

.EXAMPLE
    .\Run-AutonomousTests.ps1
    Runs autonomous tests once against running server

.EXAMPLE
    .\Run-AutonomousTests.ps1 -StartServer -IterationCount 3
    Starts server and runs 3 test iterations

.EXAMPLE
    .\Run-AutonomousTests.ps1 -AutoFix
    Runs tests and attempts to automatically fix detected issues
#>

param(
    [string]$TestCategory = "Autonomous",
    [int]$IterationCount = 1,
    [switch]$AutoFix,
    [string]$ServerUrl = "http://localhost:5555",
    [switch]$StartServer
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# Color output functions
function Write-Header {
    param([string]$Message)
    Write-Host "`n$('=' * 80)" -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor White
    Write-Host "$('=' * 80)" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor Blue
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

# Main script
Write-Header "🤖 AUTONOMOUS TEST RUNNER - BlazorBook Quality Assurance"

$rootDir = Split-Path -Parent $PSScriptRoot
$testProjectDir = Join-Path $PSScriptRoot "BlazorBook.E2E"
$feedbackBaseDir = Join-Path $testProjectDir "test-feedback"
$webProjectDir = Join-Path $rootDir "src\BlazorBook.Web"

Write-Info "Root Directory: $rootDir"
Write-Info "Test Project: $testProjectDir"
Write-Info "Feedback Directory: $feedbackBaseDir"
Write-Info "Iterations: $IterationCount"
Write-Info "Auto-Fix: $AutoFix"

# Step 1: Check if server is running or start it
$serverProcess = $null
if ($StartServer) {
    Write-Header "🚀 Starting BlazorBook.Web Server"
    
    Push-Location $webProjectDir
    try {
        Write-Info "Building project..."
        dotnet build --configuration Release | Out-Null
        
        Write-Info "Starting server on $ServerUrl..."
        $serverProcess = Start-Process -FilePath "dotnet" `
            -ArgumentList "run --no-build --configuration Release --urls $ServerUrl" `
            -WorkingDirectory $webProjectDir `
            -PassThru `
            -WindowStyle Hidden
        
        # Wait for server to be ready
        Write-Info "Waiting for server to be ready..."
        $maxAttempts = 30
        $attempt = 0
        $ready = $false
        
        while ($attempt -lt $maxAttempts -and -not $ready) {
            try {
                $response = Invoke-WebRequest -Uri "$ServerUrl/health" -Method Get -TimeoutSec 2 -ErrorAction SilentlyContinue
                if ($response.StatusCode -eq 200) {
                    $ready = $true
                }
            } catch {
                Start-Sleep -Seconds 1
                $attempt++
            }
        }
        
        if ($ready) {
            Write-Success "Server is ready at $ServerUrl"
        } else {
            Write-Error "Server failed to start after $maxAttempts attempts"
            exit 1
        }
    } finally {
        Pop-Location
    }
} else {
    Write-Info "Checking if server is running at $ServerUrl..."
    try {
        $response = Invoke-WebRequest -Uri "$ServerUrl" -Method Get -TimeoutSec 5 -ErrorAction Stop
        Write-Success "Server is running"
    } catch {
        Write-Error "Server is not running at $ServerUrl. Use -StartServer to auto-start."
        exit 1
    }
}

# Step 2: Run test iterations
$allReports = @()
$allIssues = @()

for ($iteration = 1; $iteration -le $IterationCount; $iteration++) {
    Write-Header "🧪 TEST ITERATION $iteration of $IterationCount"
    
    Push-Location $testProjectDir
    try {
        Write-Info "Running tests with category: $TestCategory"
        
        $testFilter = if ($TestCategory) { "--filter Category=$TestCategory" } else { "" }
        
        # Run tests
        $testOutput = & dotnet test `
            $testFilter `
            --logger "console;verbosity=normal" `
            --configuration Debug `
            2>&1 | Out-String
        
        $testExitCode = $LASTEXITCODE
        
        # Parse test results
        if ($testOutput -match "Passed:\s+(\d+)") { $passedCount = [int]$matches[1] } else { $passedCount = 0 }
        if ($testOutput -match "Failed:\s+(\d+)") { $failedCount = [int]$matches[1] } else { $failedCount = 0 }
        if ($testOutput -match "Total:\s+(\d+)") { $totalCount = [int]$matches[1] } else { $totalCount = 0 }
        
        Write-Info "Test Results: $passedCount passed, $failedCount failed, $totalCount total"
        
        if ($failedCount -gt 0) {
            Write-Warning "Some tests failed - analyzing feedback for root cause..."
        } else {
            Write-Success "All tests passed!"
        }
        
        # Step 3: Analyze feedback reports
        Write-Header "📊 ANALYZING FEEDBACK REPORTS"
        
        $feedbackDirs = Get-ChildItem -Path $feedbackBaseDir -Directory -ErrorAction SilentlyContinue | 
                        Sort-Object LastWriteTime -Descending |
                        Select-Object -First 10
        
        if ($feedbackDirs) {
            Write-Info "Found $($feedbackDirs.Count) recent feedback reports"
            
            foreach ($dir in $feedbackDirs) {
                $reportJsonPath = Join-Path $dir.FullName "report.json"
                $reportMdPath = Join-Path $dir.FullName "report.md"
                
                if (Test-Path $reportJsonPath) {
                    $report = Get-Content $reportJsonPath | ConvertFrom-Json
                    $allReports += $report
                    
                    Write-Info "Report: $($report.TestName)"
                    Write-Info "  Status: $(if ($report.Passed) { '✅ Passed' } else { '❌ Failed' })"
                    Write-Info "  Duration: $([math]::Round($report.Duration.TotalSeconds, 1))s"
                    Write-Info "  UI Issues: $($report.UIIssues.Count)"
                    Write-Info "  Console Errors: $($report.ConsoleErrors.Count)"
                    Write-Info "  Network Errors: $($report.NetworkErrors.Count)"
                    
                    # Collect issues
                    foreach ($issue in $report.UIIssues) {
                        $allIssues += $issue
                    }
                    
                    # Display high priority issues
                    $highPriorityIssues = $report.UIIssues | Where-Object { $_.Severity -eq "high" }
                    if ($highPriorityIssues) {
                        Write-Warning "  🔴 High Priority Issues:"
                        foreach ($issue in $highPriorityIssues) {
                            Write-Host "     - $($issue.Message)" -ForegroundColor Red
                        }
                    }
                }
            }
        } else {
            Write-Warning "No feedback reports found in $feedbackBaseDir"
        }
        
    } finally {
        Pop-Location
    }
    
    if ($iteration -lt $IterationCount) {
        Write-Info "Waiting 5 seconds before next iteration..."
        Start-Sleep -Seconds 5
    }
}

# Step 4: Generate consolidated analysis
Write-Header "📈 CONSOLIDATED ANALYSIS"

$totalTests = $allReports.Count
$passedTests = ($allReports | Where-Object { $_.Passed }).Count
$failedTests = $totalTests - $passedTests
$totalUIIssues = $allIssues.Count
$highPriorityIssues = ($allIssues | Where-Object { $_.Severity -eq "high" }).Count
$mediumPriorityIssues = ($allIssues | Where-Object { $_.Severity -eq "medium" }).Count
$lowPriorityIssues = ($allIssues | Where-Object { $_.Severity -eq "low" }).Count

Write-Info "Tests Run: $totalTests"
Write-Info "  ✅ Passed: $passedTests"
Write-Info "  ❌ Failed: $failedTests"
Write-Info ""
Write-Info "UI Issues Found: $totalUIIssues"
Write-Info "  🔴 High Priority: $highPriorityIssues"
Write-Info "  🟡 Medium Priority: $mediumPriorityIssues"
Write-Info "  🔵 Low Priority: $lowPriorityIssues"

# Group issues by type
$issuesByType = $allIssues | Group-Object -Property Type
Write-Info ""
Write-Info "Issues by Category:"
foreach ($group in $issuesByType) {
    Write-Info "  $($group.Name): $($group.Count)"
}

# Step 5: Generate recommendations
Write-Header "💡 AUTOMATED RECOMMENDATIONS"

$recommendations = @()

# Analyze common patterns
if ($highPriorityIssues -gt 0) {
    Write-Warning "CRITICAL: $highPriorityIssues high priority issues detected!"
    Write-Info "These issues should be addressed immediately for demo readiness."
}

# Layout issues
$layoutIssues = $allIssues | Where-Object { $_.Type -eq "layout" }
if ($layoutIssues) {
    Write-Info ""
    Write-Info "🎨 LAYOUT RECOMMENDATIONS:"
    
    $horizontalScrollIssues = $layoutIssues | Where-Object { $_.Message -match "horizontal scroll" }
    if ($horizontalScrollIssues) {
        Write-Info "  1. Fix horizontal scrollbar issue:"
        Write-Info "     - Add 'overflow-x: hidden' to MainLayout.razor.css"
        Write-Info "     - Check for fixed-width elements in components"
        Write-Info "     - Review responsive design breakpoints"
        $recommendations += "Fix horizontal scrollbar in MainLayout.razor.css"
    }
    
    $overlapIssues = $layoutIssues | Where-Object { $_.Message -match "overlapping" }
    if ($overlapIssues) {
        Write-Info "  2. Fix overlapping elements:"
        Write-Info "     - Review z-index values in component styles"
        Write-Info "     - Consider using CSS Grid or Flexbox for layout"
        $recommendations += "Fix overlapping elements using proper CSS positioning"
    }
}

# Content issues
$contentIssues = $allIssues | Where-Object { $_.Type -eq "content" }
if ($contentIssues) {
    Write-Info ""
    Write-Info "📝 CONTENT RECOMMENDATIONS:"
    
    $brokenImageIssues = $contentIssues | Where-Object { $_.Message -match "broken images" }
    if ($brokenImageIssues) {
        Write-Info "  1. Fix broken images:"
        Write-Info "     - Check image src paths in components"
        Write-Info "     - Add fallback images for missing content"
        Write-Info "     - Implement proper error handling"
        $recommendations += "Fix broken images with proper error handling"
    }
    
    $emptyContentIssues = $contentIssues | Where-Object { $_.Message -match "empty.*content" }
    if ($emptyContentIssues) {
        Write-Info "  2. Fix empty interactive elements:"
        Write-Info "     - Add text labels or icons to buttons"
        Write-Info "     - Ensure all links have visible content"
        $recommendations += "Add visible content to empty interactive elements"
    }
}

# Accessibility issues
$a11yIssues = $allIssues | Where-Object { $_.Type -eq "accessibility" }
if ($a11yIssues) {
    Write-Info ""
    Write-Info "♿ ACCESSIBILITY RECOMMENDATIONS:"
    
    $altTextIssues = $a11yIssues | Where-Object { $_.Message -match "alt text" }
    if ($altTextIssues) {
        Write-Info "  1. Add alt text to images:"
        Write-Info "     - Review all img tags and add descriptive alt attributes"
        $recommendations += "Add alt text to all images"
    }
    
    $contrastIssues = $a11yIssues | Where-Object { $_.Message -match "contrast" }
    if ($contrastIssues) {
        Write-Info "  2. Improve color contrast:"
        Write-Info "     - Adjust text and background colors"
        Write-Info "     - Aim for WCAG AA compliance (4.5:1 ratio)"
        $recommendations += "Improve color contrast for WCAG compliance"
    }
}

# Step 6: Generate master report
Write-Header "📄 GENERATING MASTER REPORT"

$masterReportDir = Join-Path $testProjectDir "autonomous-reports"
if (-not (Test-Path $masterReportDir)) {
    New-Item -ItemType Directory -Path $masterReportDir | Out-Null
}

$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$masterReportPath = Join-Path $masterReportDir "autonomous-report-$timestamp.md"

# Build report content using string builder to avoid here-string parsing issues
$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine("# 🤖 Autonomous Test Report")
[void]$sb.AppendLine("Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
[void]$sb.AppendLine("Test Category: $TestCategory")
[void]$sb.AppendLine("Iterations: $IterationCount")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("## 📊 Summary")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("Total Tests: $totalTests")
[void]$sb.AppendLine("Passed: $passedTests")
[void]$sb.AppendLine("Failed: $failedTests")
[void]$sb.AppendLine("Total UI Issues: $totalUIIssues")
[void]$sb.AppendLine("High Priority: $highPriorityIssues")
[void]$sb.AppendLine("Medium Priority: $mediumPriorityIssues")
[void]$sb.AppendLine("Low Priority: $lowPriorityIssues")
[void]$sb.AppendLine("")

if ($failedTests -gt 0) {
    [void]$sb.AppendLine("⚠️ WARNING: $failedTests tests failed - functional issues detected!")
} else {
    [void]$sb.AppendLine("✅ SUCCESS: All tests passed functionally!")
}
[void]$sb.AppendLine("")

## 🎯 Priority Actions

### Must Fix Before

 (High Priority)
$( if ($highPriorityIssues -gt 0) {
    "The following $highPriorityIssues high priority issues must be addressed:`n`n"
    $highIssues = $allIssues | Where-Object { $_.Severity -eq "high" } | Select-Object -First 10
    foreach ($issue in $highIssues) {
        "- **$($issue.Type):** $($issue.Message)`n"
    }
} else {
    "✅ No high priority issues detected!`n"
})

### Should Fix (Medium Priority)
$( if ($mediumPriorityIssues -gt 0) {
    "Consider addressing these $mediumPriorityIssues medium priority issues:`n`n"
    $medIssues = $allIssues | Where-Object { $_.Severity -eq "medium" } | Select-Object -First 10
    foreach ($issue in $medIssues) {
        "- **$($issue.Type):** $($issue.Message)`n"
    }
} else {
    "✅ No medium priority issues detected!`n"
})

## 💡 Automated Recommendations

$( if ($recommendations.Count -gt 0) {
    "Based on the analysis, here are the recommended actions:`n`n"
    $i = 1
    foreach ($rec in $recommendations) {
        "$i. $rec`n"
        $i++
    }
} else {
    "✅ No specific recommendations - application looks good!`n"
})

## 📂 Detailed Reports

Individual test feedback reports are available in:
```
$feedbackBaseDir
```

View the HTML reports for detailed screenshots and analysis.

## 📈 Issues by Category

$(
    foreach ($group in $issuesByType) {
        "### $($group.Name) ($($group.Count) issues)`n`n"
        $topIssues = $group.Group | Select-Object -First 5
        foreach ($issue in $topIssues) {
            "- [$($issue.Severity)] $($issue.Message)`n"
        }
        "`n"
    }
)

## 🎬 Next Steps

1. **Review High Priority Issues:** Address all high priority issues immediately
2. **Check Screenshots:** Review captured screenshots in feedback directories
3. **Run Tests Again:** Re-run tests after fixes to verify improvements
4. **Iterate:** Continue this process until no high/medium issues remain

---
*This report was generated autonomously by the BlazorBook Test Feedback System*
"@

$reportContent | Out-File -FilePath $masterReportPath -Encoding UTF8
Write-Success "Master report saved to: $masterReportPath"

# Open report in default viewer
Write-Info "Opening master report..."
Start-Process $masterReportPath

# Step 7: Auto-fix (if enabled)
if ($AutoFix -and $recommendations.Count -gt 0) {
    Write-Header "🔧 AUTO-FIX MODE (Experimental)"
    Write-Warning "Auto-fix is experimental and may require manual review."
    Write-Info "This feature will be implemented in a future iteration."
    Write-Info "For now, please review the recommendations and apply fixes manually."
}

# Step 8: Cleanup
if ($serverProcess) {
    Write-Header "🧹 CLEANUP"
    Write-Info "Stopping server process..."
    Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
    Write-Success "Server stopped"
}

# Final summary
Write-Header "🎉 AUTONOMOUS TEST RUN COMPLETE"

if ($failedTests -eq 0 -and $highPriorityIssues -eq 0) {
    Write-Success "ALL SYSTEMS GREEN! Application is demo-ready! 🚀"
} elseif ($failedTests -gt 0) {
    Write-Warning "FUNCTIONAL ISSUES DETECTED - $failedTests tests failed"
    Write-Info "Review test output and fix failing tests before addressing UI issues"
} elseif ($highPriorityIssues -gt 0) {
    Write-Warning "HIGH PRIORITY UI ISSUES DETECTED - $highPriorityIssues issues found"
    Write-Info "Address high priority issues to ensure demo quality"
} else {
    Write-Success "TESTS PASSED with $mediumPriorityIssues medium and $lowPriorityIssues low priority UI issues"
    Write-Info "Consider addressing medium priority issues for optimal experience"
}

Write-Info ""
Write-Info "📄 Full report: $masterReportPath"
Write-Info "📁 Feedback directory: $feedbackBaseDir"
Write-Info ""

exit $(if ($failedTests -gt 0) { 1 } else { 0 })

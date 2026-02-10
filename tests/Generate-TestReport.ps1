<#
.SYNOPSIS
    Generates an HTML test report with embedded screenshots.

.DESCRIPTION
    Parses TRX test results and generates a comprehensive HTML report
    with embedded screenshots, test statistics, and detailed results.

.PARAMETER ResultsDir
    Directory containing TRX test result files.

.PARAMETER ScreenshotsDir
    Directory containing test screenshots.

.PARAMETER Timestamp
    Timestamp for the report filename.

.EXAMPLE
    .\Generate-TestReport.ps1 -ResultsDir "TestResults" -ScreenshotsDir "screenshots"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ResultsDir,
    
    [Parameter(Mandatory = $true)]
    [string]$ScreenshotsDir,
    
    [string]$Timestamp = (Get-Date -Format "yyyy-MM-dd_HH-mm-ss")
)

$ErrorActionPreference = "Stop"

# Find the latest TRX file
$trxFiles = Get-ChildItem -Path $ResultsDir -Filter "*.trx" | Sort-Object LastWriteTime -Descending

if ($trxFiles.Count -eq 0) {
    Write-Warning "No TRX files found in $ResultsDir"
    exit 1
}

$trxFile = $trxFiles[0]
Write-Host "Processing: $($trxFile.Name)" -ForegroundColor Cyan

# Parse TRX file
[xml]$trx = Get-Content $trxFile.FullName

$ns = @{
    ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"
}

# Extract test results
$testResults = Select-Xml -Xml $trx -XPath "//ns:UnitTestResult" -Namespace $ns | ForEach-Object {
    $result = $_.Node
    
    [PSCustomObject]@{
        TestName    = $result.testName
        Outcome     = $result.outcome
        Duration    = $result.duration
        StartTime   = $result.startTime
        EndTime     = $result.endTime
        ErrorMessage = $result.Output.ErrorInfo.Message
        StackTrace  = $result.Output.ErrorInfo.StackTrace
        StdOut      = $result.Output.StdOut
    }
}

# Calculate statistics
$totalTests = $testResults.Count
$passedTests = ($testResults | Where-Object { $_.Outcome -eq "Passed" }).Count
$failedTests = ($testResults | Where-Object { $_.Outcome -eq "Failed" }).Count
$skippedTests = ($testResults | Where-Object { $_.Outcome -eq "NotExecuted" }).Count

$passRate = if ($totalTests -gt 0) { [math]::Round(($passedTests / $totalTests) * 100, 2) } else { 0 }

# Get screenshots
$screenshots = @()
if (Test-Path $ScreenshotsDir) {
    $screenshots = Get-ChildItem -Path $ScreenshotsDir -Filter "*.png" -Recurse | ForEach-Object {
        [PSCustomObject]@{
            Name = $_.Name
            Path = $_.FullName
            RelativePath = $_.FullName.Replace($ScreenshotsDir, "screenshots").Replace("\", "/")
            Size = [math]::Round($_.Length / 1KB, 2)
        }
    }
}

# Group screenshots by category
$screenshotsByCategory = $screenshots | Group-Object { 
    if ($_.Name -match "^(\w+)-") { $matches[1] } else { "other" }
}

# Generate HTML report
$html = @"
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>BlazorBook E2E Test Report - $Timestamp</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 20px;
            color: #333;
        }

        .container {
            max-width: 1400px;
            margin: 0 auto;
            background: white;
            border-radius: 12px;
            box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
            overflow: hidden;
        }

        .header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 40px;
            text-align: center;
        }

        .header h1 {
            font-size: 2.5em;
            margin-bottom: 10px;
        }

        .header p {
            font-size: 1.1em;
            opacity: 0.9;
        }

        .stats {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
            padding: 40px;
            background: #f8f9fa;
        }

        .stat-card {
            background: white;
            padding: 25px;
            border-radius: 8px;
            box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
            text-align: center;
            transition: transform 0.2s;
        }

        .stat-card:hover {
            transform: translateY(-5px);
        }

        .stat-card .number {
            font-size: 3em;
            font-weight: bold;
            margin: 10px 0;
        }

        .stat-card .label {
            color: #666;
            font-size: 0.9em;
            text-transform: uppercase;
            letter-spacing: 1px;
        }

        .stat-card.passed .number { color: #28a745; }
        .stat-card.failed .number { color: #dc3545; }
        .stat-card.skipped .number { color: #ffc107; }
        .stat-card.total .number { color: #667eea; }

        .content {
            padding: 40px;
        }

        .section {
            margin-bottom: 40px;
        }

        .section-title {
            font-size: 1.8em;
            color: #667eea;
            margin-bottom: 20px;
            padding-bottom: 10px;
            border-bottom: 3px solid #667eea;
        }

        .test-results {
            margin-top: 20px;
        }

        .test-result {
            background: #f8f9fa;
            border-left: 4px solid #667eea;
            padding: 15px;
            margin-bottom: 15px;
            border-radius: 4px;
            transition: all 0.2s;
        }

        .test-result:hover {
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
        }

        .test-result.passed { border-left-color: #28a745; }
        .test-result.failed { border-left-color: #dc3545; background: #fff5f5; }
        .test-result.skipped { border-left-color: #ffc107; }

        .test-result .test-name {
            font-weight: 600;
            font-size: 1.1em;
            margin-bottom: 5px;
        }

        .test-result .test-info {
            color: #666;
            font-size: 0.9em;
        }

        .test-result .test-duration {
            display: inline-block;
            background: #e9ecef;
            padding: 3px 10px;
            border-radius: 12px;
            font-size: 0.85em;
            margin-left: 10px;
        }

        .test-result .error-message {
            margin-top: 10px;
            padding: 10px;
            background: #fee;
            border-radius: 4px;
            font-family: 'Courier New', monospace;
            font-size: 0.9em;
            color: #c00;
        }

        .screenshot-gallery {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
            gap: 20px;
            margin-top: 20px;
        }

        .screenshot-item {
            background: #f8f9fa;
            border-radius: 8px;
            overflow: hidden;
            box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
            transition: transform 0.2s;
        }

        .screenshot-item:hover {
            transform: scale(1.02);
            box-shadow: 0 4px 16px rgba(0, 0, 0, 0.2);
        }

        .screenshot-item img {
            width: 100%;
            height: 200px;
            object-fit: cover;
            cursor: pointer;
        }

        .screenshot-item .caption {
            padding: 12px;
            font-size: 0.9em;
            color: #555;
            text-align: center;
        }

        .category-section {
            margin-bottom: 30px;
        }

        .category-title {
            font-size: 1.3em;
            color: #495057;
            margin-bottom: 15px;
            padding: 10px;
            background: #e9ecef;
            border-radius: 4px;
        }

        .badge {
            display: inline-block;
            padding: 4px 8px;
            border-radius: 12px;
            font-size: 0.8em;
            font-weight: 600;
            text-transform: uppercase;
        }

        .badge-passed { background: #d4edda; color: #155724; }
        .badge-failed { background: #f8d7da; color: #721c24; }
        .badge-skipped { background: #fff3cd; color: #856404; }

        .progress-bar {
            height: 30px;
            background: #e9ecef;
            border-radius: 15px;
            overflow: hidden;
            margin: 20px 0;
        }

        .progress-fill {
            height: 100%;
            background: linear-gradient(90deg, #28a745 0%, #20c997 100%);
            display: flex;
            align-items: center;
            justify-content: center;
            color: white;
            font-weight: 600;
            transition: width 1s ease;
        }

        /* Modal for full-size screenshots */
        .modal {
            display: none;
            position: fixed;
            z-index: 1000;
            left: 0;
            top: 0;
            width: 100%;
            height: 100%;
            background: rgba(0, 0, 0, 0.9);
        }

        .modal-content {
            margin: auto;
            display: block;
            max-width: 90%;
            max-height: 90%;
            margin-top: 2%;
        }

        .close {
            position: absolute;
            top: 15px;
            right: 35px;
            color: #f1f1f1;
            font-size: 40px;
            font-weight: bold;
            cursor: pointer;
        }

        .footer {
            background: #f8f9fa;
            padding: 20px;
            text-align: center;
            color: #666;
            font-size: 0.9em;
        }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>📊 BlazorBook E2E Test Report</h1>
            <p>Generated on $Timestamp</p>
        </div>

        <div class="stats">
            <div class="stat-card total">
                <div class="label">Total Tests</div>
                <div class="number">$totalTests</div>
            </div>
            <div class="stat-card passed">
                <div class="label">Passed</div>
                <div class="number">$passedTests</div>
            </div>
            <div class="stat-card failed">
                <div class="label">Failed</div>
                <div class="number">$failedTests</div>
            </div>
            <div class="stat-card skipped">
                <div class="label">Skipped</div>
                <div class="number">$skippedTests</div>
            </div>
        </div>

        <div class="content">
            <div class="section">
                <h2 class="section-title">Pass Rate</h2>
                <div class="progress-bar">
                    <div class="progress-fill" style="width: $passRate%">
                        $passRate%
                    </div>
                </div>
            </div>

            <div class="section">
                <h2 class="section-title">Test Results</h2>
                <div class="test-results">
"@

# Add test results
foreach ($test in $testResults | Sort-Object Outcome, TestName) {
    $outcomeClass = $test.Outcome.ToLower()
    $badgeClass = "badge-" + $outcomeClass
    
    $html += @"
                    <div class="test-result $outcomeClass">
                        <div class="test-name">
                            <span class="badge $badgeClass">$($test.Outcome)</span>
                            $($test.TestName)
                            <span class="test-duration">⏱ $($test.Duration)</span>
                        </div>
"@
    
    if ($test.ErrorMessage) {
        $html += @"
                        <div class="error-message">
                            <strong>Error:</strong> $([System.Web.HttpUtility]::HtmlEncode($test.ErrorMessage))
                        </div>
"@
    }
    
    $html += @"
                    </div>
"@
}

$html += @"
                </div>
            </div>

            <div class="section">
                <h2 class="section-title">Screenshots ($($screenshots.Count))</h2>
"@

# Add screenshots grouped by category
foreach ($category in $screenshotsByCategory) {
    $html += @"
                <div class="category-section">
                    <h3 class="category-title">$($category.Name) Screenshots</h3>
                    <div class="screenshot-gallery">
"@
    
    foreach ($screenshot in $category.Group) {
        # Convert image to base64 for embedding
        $imageBytes = [System.IO.File]::ReadAllBytes($screenshot.Path)
        $base64 = [Convert]::ToBase64String($imageBytes)
        
        $html += @"
                        <div class="screenshot-item">
                            <img src="data:image/png;base64,$base64" alt="$($screenshot.Name)" onclick="openModal(this.src)">
                            <div class="caption">
                                $($screenshot.Name)<br>
                                <small>$($screenshot.Size) KB</small>
                            </div>
                        </div>
"@
    }
    
    $html += @"
                    </div>
                </div>
"@
}

$html += @"
            </div>
        </div>

        <div class="footer">
            <p>BlazorBook E2E Test Report | Generated with ❤️ by Playwright</p>
        </div>
    </div>

    <!-- Modal for full-size images -->
    <div id="imageModal" class="modal" onclick="closeModal()">
        <span class="close" onclick="closeModal()">&times;</span>
        <img class="modal-content" id="modalImage">
    </div>

    <script>
        function openModal(src) {
            document.getElementById('imageModal').style.display = 'block';
            document.getElementById('modalImage').src = src;
        }

        function closeModal() {
            document.getElementById('imageModal').style.display = 'none';
        }

        // Animate progress bar on load
        window.addEventListener('load', function() {
            const progressFill = document.querySelector('.progress-fill');
            if (progressFill) {
                const width = progressFill.style.width;
                progressFill.style.width = '0%';
                setTimeout(() => {
                    progressFill.style.width = width;
                }, 100);
            }
        });
    </script>
</body>
</html>
"@

# Save HTML report
$reportPath = Join-Path $ResultsDir "test-report-$Timestamp.html"
$html | Out-File -FilePath $reportPath -Encoding UTF8

Write-Host "✓ HTML report generated: $reportPath" -ForegroundColor Green

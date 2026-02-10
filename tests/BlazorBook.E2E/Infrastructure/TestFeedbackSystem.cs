using Microsoft.Playwright;
using System.Text;
using System.Text.Json;

namespace BlazorBook.E2E.Infrastructure;

/// <summary>
/// Autonomous test feedback system that captures detailed information
/// for self-diagnosis and improvement without human intervention.
/// </summary>
public class TestFeedbackSystem
{
    private readonly string _feedbackDir;
    private readonly string _screenshotDir;
    private readonly string _logsDir;
    private readonly List<TestFeedback> _feedbacks;
    private readonly IPage _page;

    public TestFeedbackSystem(IPage page, string testName)
    {
        _page = page;
        _feedbackDir = Path.Combine("test-feedback", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
        _screenshotDir = Path.Combine(_feedbackDir, "screenshots");
        _logsDir = Path.Combine(_feedbackDir, "logs");
        _feedbacks = new List<TestFeedback>();

        Directory.CreateDirectory(_feedbackDir);
        Directory.CreateDirectory(_screenshotDir);
        Directory.CreateDirectory(_logsDir);

        // Start capturing console logs
        _page.Console += OnConsoleMessage;
        _page.PageError += OnPageError;
        _page.Request += OnRequest;
        _page.Response += OnResponse;
    }

    public async Task<string> CaptureScreenshotAsync(string name, string description = "")
    {
        var filename = $"{DateTime.Now:HHmmss}-{SanitizeFileName(name)}.png";
        var path = Path.Combine(_screenshotDir, filename);
        
        await _page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = path,
            FullPage = true
        });

        LogInfo($"📸 Screenshot captured: {name} - {description}");
        return path;
    }

    public async Task<TestFeedback> CaptureStateAsync(string checkpoint, string status = "info")
    {
        var feedback = new TestFeedback
        {
            Checkpoint = checkpoint,
            Timestamp = DateTime.Now,
            Status = status,
            Url = _page.Url,
            Title = await _page.TitleAsync()
        };

        // Capture screenshot
        feedback.ScreenshotPath = await CaptureScreenshotAsync(checkpoint);

        // Capture DOM snapshot
        feedback.DomSnapshot = await CaptureDomSnapshotAsync();

        // Capture visible text
        feedback.VisibleText = await CaptureVisibleTextAsync();

        // Capture computed styles for key elements
        feedback.StyleIssues = await DetectStyleIssuesAsync();

        // Capture accessibility tree
        feedback.AccessibilitySnapshot = await CaptureAccessibilitySnapshotAsync();

        _feedbacks.Add(feedback);
        
        LogInfo($"✅ State captured at: {checkpoint}");
        return feedback;
    }

    public async Task<List<UIIssue>> AnalyzeUIIssuesAsync()
    {
        var issues = new List<UIIssue>();

        // Check for broken layouts
        var layoutIssues = await DetectLayoutIssuesAsync();
        issues.AddRange(layoutIssues);

        // Check for missing content
        var contentIssues = await DetectContentIssuesAsync();
        issues.AddRange(contentIssues);

        // Check for interaction problems
        var interactionIssues = await DetectInteractionIssuesAsync();
        issues.AddRange(interactionIssues);

        // Check for accessibility issues
        var a11yIssues = await DetectAccessibilityIssuesAsync();
        issues.AddRange(a11yIssues);

        return issues;
    }

    public async Task<TestReport> GenerateReportAsync(string testName, bool passed, string? error = null)
    {
        var report = new TestReport
        {
            TestName = testName,
            Passed = passed,
            Error = error,
            Feedbacks = _feedbacks,
            UIIssues = await AnalyzeUIIssuesAsync(),
            ConsoleErrors = _consoleErrors,
            NetworkErrors = _networkErrors,
            Duration = _feedbacks.Any() ? 
                _feedbacks.Last().Timestamp - _feedbacks.First().Timestamp : 
                TimeSpan.Zero
        };

        // Save as JSON
        var jsonPath = Path.Combine(_feedbackDir, "report.json");
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        await File.WriteAllTextAsync(jsonPath, json);

        // Save as HTML
        var htmlPath = Path.Combine(_feedbackDir, "report.html");
        await File.WriteAllTextAsync(htmlPath, GenerateHtmlReport(report));

        // Save as markdown for AI consumption
        var mdPath = Path.Combine(_feedbackDir, "report.md");
        await File.WriteAllTextAsync(mdPath, GenerateMarkdownReport(report));

        LogInfo($"📊 Report generated: {_feedbackDir}");
        return report;
    }

    private async Task<string> CaptureDomSnapshotAsync()
    {
        try
        {
            return await _page.ContentAsync();
        }
        catch
        {
            return "<!-- Unable to capture DOM -->";
        }
    }

    private async Task<string> CaptureVisibleTextAsync()
    {
        try
        {
            return await _page.EvaluateAsync<string>(@"
                () => {
                    const walker = document.createTreeWalker(
                        document.body,
                        NodeFilter.SHOW_TEXT,
                        null
                    );
                    let text = '';
                    let node;
                    while (node = walker.nextNode()) {
                        const parent = node.parentElement;
                        if (parent && getComputedStyle(parent).display !== 'none') {
                            text += node.textContent?.trim() + '\n';
                        }
                    }
                    return text;
                }
            ");
        }
        catch
        {
            return "";
        }
    }

    private async Task<List<StyleIssue>> DetectStyleIssuesAsync()
    {
        var issues = new List<StyleIssue>();

        try
        {
            var elements = await _page.QuerySelectorAllAsync("*");
            
            foreach (var element in elements.Take(100)) // Sample first 100 elements
            {
                var tagName = await element.EvaluateAsync<string>("el => el.tagName");
                var computed = await element.EvaluateAsync<Dictionary<string, string>>(@"
                    el => {
                        const style = getComputedStyle(el);
                        return {
                            display: style.display,
                            visibility: style.visibility,
                            opacity: style.opacity,
                            width: style.width,
                            height: style.height,
                            overflow: style.overflow,
                            position: style.position,
                            zIndex: style.zIndex
                        };
                    }
                ");

                // Check for invisible elements that might be bugs
                if (computed["visibility"] == "hidden" && computed["display"] != "none")
                {
                    issues.Add(new StyleIssue 
                    { 
                        Element = tagName, 
                        Issue = "Element is visibility:hidden",
                        Severity = "warning"
                    });
                }

                // Check for elements with 0 dimensions
                if (computed["width"] == "0px" && computed["height"] == "0px")
                {
                    issues.Add(new StyleIssue 
                    { 
                        Element = tagName, 
                        Issue = "Element has 0 width and height",
                        Severity = "info"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            LogError($"Style detection error: {ex.Message}");
        }

        return issues;
    }

    private async Task<string> CaptureAccessibilitySnapshotAsync()
    {
        try
        {
            var snapshot = await _page.Accessibility.SnapshotAsync();
            return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return "{}";
        }
    }

    private async Task<List<UIIssue>> DetectLayoutIssuesAsync()
    {
        var issues = new List<UIIssue>();

        try
        {
            // Check for horizontal scrollbars (often indicates layout issues)
            var hasHorizontalScroll = await _page.EvaluateAsync<bool>(@"
                () => document.documentElement.scrollWidth > document.documentElement.clientWidth
            ");

            if (hasHorizontalScroll)
            {
                issues.Add(new UIIssue
                {
                    Type = "layout",
                    Severity = "high",
                    Message = "Page has horizontal scrollbar (potential layout overflow)",
                    Screenshot = await CaptureScreenshotAsync("horizontal-scroll-issue")
                });
            }

            // Check for overlapping elements
            var overlaps = await _page.EvaluateAsync<int>(@"
                () => {
                    const elements = Array.from(document.querySelectorAll('button, a, input'));
                    let count = 0;
                    for (let i = 0; i < elements.length; i++) {
                        const rect1 = elements[i].getBoundingClientRect();
                        for (let j = i + 1; j < elements.length; j++) {
                            const rect2 = elements[j].getBoundingClientRect();
                            if (rect1.left < rect2.right && rect1.right > rect2.left &&
                                rect1.top < rect2.bottom && rect1.bottom > rect2.top) {
                                count++;
                            }
                        }
                    }
                    return count;
                }
            ");

            if (overlaps > 0)
            {
                issues.Add(new UIIssue
                {
                    Type = "layout",
                    Severity = "medium",
                    Message = $"Found {overlaps} overlapping interactive elements",
                    Screenshot = await CaptureScreenshotAsync("overlapping-elements")
                });
            }
        }
        catch (Exception ex)
        {
            LogError($"Layout detection error: {ex.Message}");
        }

        return issues;
    }

    private async Task<List<UIIssue>> DetectContentIssuesAsync()
    {
        var issues = new List<UIIssue>();

        try
        {
            // Check for empty text in buttons/links
            var emptyInteractives = await _page.EvaluateAsync<int>(@"
                () => {
                    const elements = document.querySelectorAll('button, a');
                    return Array.from(elements).filter(el => 
                        !el.textContent?.trim() && 
                        !el.querySelector('img, svg')
                    ).length;
                }
            ");

            if (emptyInteractives > 0)
            {
                issues.Add(new UIIssue
                {
                    Type = "content",
                    Severity = "medium",
                    Message = $"Found {emptyInteractives} buttons/links with no visible content",
                    Screenshot = await CaptureScreenshotAsync("empty-interactives")
                });
            }

            // Check for broken images
            var brokenImages = await _page.EvaluateAsync<int>(@"
                () => {
                    const images = document.querySelectorAll('img');
                    return Array.from(images).filter(img => !img.complete || img.naturalHeight === 0).length;
                }
            ");

            if (brokenImages > 0)
            {
                issues.Add(new UIIssue
                {
                    Type = "content",
                    Severity = "high",
                    Message = $"Found {brokenImages} broken images",
                    Screenshot = await CaptureScreenshotAsync("broken-images")
                });
            }
        }
        catch (Exception ex)
        {
            LogError($"Content detection error: {ex.Message}");
        }

        return issues;
    }

    private async Task<List<UIIssue>> DetectInteractionIssuesAsync()
    {
        var issues = new List<UIIssue>();

        try
        {
            // Check for buttons without click handlers
            var inactiveButtons = await _page.EvaluateAsync<int>(@"
                () => {
                    const buttons = document.querySelectorAll('button');
                    return Array.from(buttons).filter(btn => 
                        !btn.onclick && 
                        !btn.hasAttribute('type') &&
                        !btn.form
                    ).length;
                }
            ");

            if (inactiveButtons > 0)
            {
                issues.Add(new UIIssue
                {
                    Type = "interaction",
                    Severity = "low",
                    Message = $"Found {inactiveButtons} buttons that might not have handlers",
                    Screenshot = await CaptureScreenshotAsync("inactive-buttons")
                });
            }

            // Check for disabled inputs
            var disabledInputs = await _page.EvaluateAsync<int>(@"
                () => document.querySelectorAll('input:disabled, button:disabled').length
            ");

            if (disabledInputs > 3) // More than 3 is suspicious
            {
                issues.Add(new UIIssue
                {
                    Type = "interaction",
                    Severity = "medium",
                    Message = $"Found {disabledInputs} disabled form elements (might indicate form state issues)",
                    Screenshot = await CaptureScreenshotAsync("many-disabled-inputs")
                });
            }
        }
        catch (Exception ex)
        {
            LogError($"Interaction detection error: {ex.Message}");
        }

        return issues;
    }

    private async Task<List<UIIssue>> DetectAccessibilityIssuesAsync()
    {
        var issues = new List<UIIssue>();

        try
        {
            // Check for images without alt text
            var imagesNoAlt = await _page.EvaluateAsync<int>(@"
                () => {
                    const images = document.querySelectorAll('img');
                    return Array.from(images).filter(img => !img.alt).length;
                }
            ");

            if (imagesNoAlt > 0)
            {
                issues.Add(new UIIssue
                {
                    Type = "accessibility",
                    Severity = "medium",
                    Message = $"Found {imagesNoAlt} images without alt text",
                    Screenshot = await CaptureScreenshotAsync("images-no-alt")
                });
            }

            // Check for low contrast text
            var lowContrast = await _page.EvaluateAsync<int>(@"
                () => {
                    const getContrast = (fg, bg) => {
                        const getLuminance = (color) => {
                            const rgb = color.match(/\d+/g).map(Number);
                            const [r, g, b] = rgb.map(val => {
                                val = val / 255;
                                return val <= 0.03928 ? val / 12.92 : Math.pow((val + 0.055) / 1.055, 2.4);
                            });
                            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
                        };
                        const l1 = getLuminance(fg) + 0.05;
                        const l2 = getLuminance(bg) + 0.05;
                        return l1 > l2 ? l1 / l2 : l2 / l1;
                    };

                    const elements = document.querySelectorAll('p, span, a, button, h1, h2, h3, h4, h5, h6');
                    let count = 0;
                    elements.forEach(el => {
                        const style = getComputedStyle(el);
                        const color = style.color;
                        const bgColor = style.backgroundColor;
                        if (color && bgColor && color !== 'rgba(0, 0, 0, 0)' && bgColor !== 'rgba(0, 0, 0, 0)') {
                            const contrast = getContrast(color, bgColor);
                            if (contrast < 4.5) count++;
                        }
                    });
                    return count;
                }
            ");

            if (lowContrast > 0)
            {
                issues.Add(new UIIssue
                {
                    Type = "accessibility",
                    Severity = "medium",
                    Message = $"Found {lowContrast} elements with potentially low contrast",
                    Screenshot = await CaptureScreenshotAsync("low-contrast")
                });
            }
        }
        catch (Exception ex)
        {
            LogError($"Accessibility detection error: {ex.Message}");
        }

        return issues;
    }

    private string GenerateHtmlReport(TestReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head>");
        sb.AppendLine("<title>Test Feedback Report</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(@"
            body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 20px; background: #f5f5f5; }
            .container { max-width: 1400px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
            h1 { color: #2c3e50; border-bottom: 3px solid #3498db; padding-bottom: 10px; }
            h2 { color: #34495e; margin-top: 30px; }
            .status { display: inline-block; padding: 5px 15px; border-radius: 20px; font-weight: bold; }
            .passed { background: #2ecc71; color: white; }
            .failed { background: #e74c3c; color: white; }
            .checkpoint { background: #ecf0f1; padding: 20px; margin: 15px 0; border-left: 4px solid #3498db; border-radius: 4px; }
            .screenshot { max-width: 100%; border: 1px solid #ddd; margin: 10px 0; cursor: pointer; transition: transform 0.2s; }
            .screenshot:hover { transform: scale(1.02); }
            .issue { padding: 15px; margin: 10px 0; border-radius: 4px; border-left: 4px solid; }
            .issue.high { border-color: #e74c3c; background: #fadbd8; }
            .issue.medium { border-color: #f39c12; background: #fef5e7; }
            .issue.low { border-color: #3498db; background: #d6eaf8; }
            .metrics { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 15px; margin: 20px 0; }
            .metric { background: #3498db; color: white; padding: 20px; border-radius: 4px; text-align: center; }
            .metric-value { font-size: 2em; font-weight: bold; }
            .timeline { position: relative; padding-left: 30px; }
            .timeline-item { position: relative; padding-bottom: 20px; }
            .timeline-item::before { content: ''; position: absolute; left: -24px; top: 0; width: 12px; height: 12px; border-radius: 50%; background: #3498db; }
            .code { background: #2c3e50; color: #ecf0f1; padding: 15px; border-radius: 4px; overflow-x: auto; font-family: 'Courier New', monospace; }
            .error { color: #e74c3c; background: #fadbd8; padding: 10px; border-radius: 4px; }
        ");
        sb.AppendLine("</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine("<div class='container'>");

        // Header
        sb.AppendLine($"<h1>🧪 Test Feedback Report: {report.TestName}</h1>");
        sb.AppendLine($"<div class='status {(report.Passed ? "passed" : "failed")}'>");
        sb.AppendLine(report.Passed ? "✅ PASSED" : "❌ FAILED");
        sb.AppendLine("</div>");

        if (!string.IsNullOrEmpty(report.Error))
        {
            sb.AppendLine($"<div class='error'><strong>Error:</strong> {report.Error}</div>");
        }

        // Metrics
        sb.AppendLine("<div class='metrics'>");
        sb.AppendLine($"<div class='metric'><div class='metric-value'>{report.Feedbacks.Count}</div><div>Checkpoints</div></div>");
        sb.AppendLine($"<div class='metric'><div class='metric-value'>{report.UIIssues.Count}</div><div>UI Issues</div></div>");
        sb.AppendLine($"<div class='metric'><div class='metric-value'>{report.ConsoleErrors.Count}</div><div>Console Errors</div></div>");
        sb.AppendLine($"<div class='metric'><div class='metric-value'>{report.Duration.TotalSeconds:F1}s</div><div>Duration</div></div>");
        sb.AppendLine("</div>");

        // UI Issues
        if (report.UIIssues.Any())
        {
            sb.AppendLine("<h2>🎨 UI Issues Detected</h2>");
            foreach (var issue in report.UIIssues.OrderByDescending(i => i.Severity))
            {
                sb.AppendLine($"<div class='issue {issue.Severity}'>");
                sb.AppendLine($"<strong>{issue.Type.ToUpper()}</strong>: {issue.Message}");
                if (!string.IsNullOrEmpty(issue.Screenshot) && File.Exists(issue.Screenshot))
                {
                    var relPath = Path.GetRelativePath(_feedbackDir, issue.Screenshot).Replace("\\", "/");
                    sb.AppendLine($"<br><img src='{relPath}' class='screenshot' />");
                }
                sb.AppendLine("</div>");
            }
        }

        // Timeline
        sb.AppendLine("<h2>📝 Test Timeline</h2>");
        sb.AppendLine("<div class='timeline'>");
        foreach (var feedback in report.Feedbacks)
        {
            sb.AppendLine("<div class='timeline-item'>");
            sb.AppendLine($"<div class='checkpoint'>");
            sb.AppendLine($"<strong>{feedback.Checkpoint}</strong> - {feedback.Timestamp:HH:mm:ss}");
            sb.AppendLine($"<br>URL: {feedback.Url}");
            sb.AppendLine($"<br>Title: {feedback.Title}");
            
            if (!string.IsNullOrEmpty(feedback.ScreenshotPath) && File.Exists(feedback.ScreenshotPath))
            {
                var relPath = Path.GetRelativePath(_feedbackDir, feedback.ScreenshotPath).Replace("\\", "/");
                sb.AppendLine($"<br><img src='{relPath}' class='screenshot' />");
            }
            
            if (feedback.StyleIssues.Any())
            {
                sb.AppendLine($"<br><strong>Style Issues:</strong>");
                sb.AppendLine("<ul>");
                foreach (var styleIssue in feedback.StyleIssues.Take(5))
                {
                    sb.AppendLine($"<li>{styleIssue.Element}: {styleIssue.Issue}</li>");
                }
                sb.AppendLine("</ul>");
            }
            
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
        }
        sb.AppendLine("</div>");

        // Console Errors
        if (report.ConsoleErrors.Any())
        {
            sb.AppendLine("<h2>🐛 Console Errors</h2>");
            foreach (var error in report.ConsoleErrors.Take(20))
            {
                sb.AppendLine($"<div class='code'>[{error.Timestamp:HH:mm:ss}] {error.Message}</div>");
            }
        }

        // Network Errors
        if (report.NetworkErrors.Any())
        {
            sb.AppendLine("<h2>🌐 Network Errors</h2>");
            foreach (var error in report.NetworkErrors.Take(20))
            {
                sb.AppendLine($"<div class='code'>[{error.Timestamp:HH:mm:ss}] {error.Url} - {error.Status}</div>");
            }
        }

        sb.AppendLine("</div></body></html>");
        return sb.ToString();
    }

    private string GenerateMarkdownReport(TestReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Test Feedback Report: {report.TestName}");
        sb.AppendLine();
        sb.AppendLine($"**Status:** {(report.Passed ? "✅ PASSED" : "❌ FAILED")}");
        sb.AppendLine($"**Duration:** {report.Duration.TotalSeconds:F1}s");
        sb.AppendLine($"**Checkpoints:** {report.Feedbacks.Count}");
        sb.AppendLine($"**UI Issues:** {report.UIIssues.Count}");
        sb.AppendLine($"**Console Errors:** {report.ConsoleErrors.Count}");
        sb.AppendLine($"**Network Errors:** {report.NetworkErrors.Count}");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(report.Error))
        {
            sb.AppendLine("## Error");
            sb.AppendLine("```");
            sb.AppendLine(report.Error);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        if (report.UIIssues.Any())
        {
            sb.AppendLine("## UI Issues (Priority Ordered)");
            sb.AppendLine();
            
            var highPriority = report.UIIssues.Where(i => i.Severity == "high").ToList();
            var medPriority = report.UIIssues.Where(i => i.Severity == "medium").ToList();
            var lowPriority = report.UIIssues.Where(i => i.Severity == "low").ToList();

            if (highPriority.Any())
            {
                sb.AppendLine("### 🔴 High Priority");
                foreach (var issue in highPriority)
                {
                    sb.AppendLine($"- **{issue.Type}**: {issue.Message}");
                    if (!string.IsNullOrEmpty(issue.Screenshot))
                        sb.AppendLine($"  - Screenshot: `{issue.Screenshot}`");
                }
                sb.AppendLine();
            }

            if (medPriority.Any())
            {
                sb.AppendLine("### 🟡 Medium Priority");
                foreach (var issue in medPriority)
                {
                    sb.AppendLine($"- **{issue.Type}**: {issue.Message}");
                }
                sb.AppendLine();
            }

            if (lowPriority.Any())
            {
                sb.AppendLine("### 🔵 Low Priority");
                foreach (var issue in lowPriority)
                {
                    sb.AppendLine($"- **{issue.Type}**: {issue.Message}");
                }
                sb.AppendLine();
            }
        }

        sb.AppendLine("## Test Timeline");
        sb.AppendLine();
        foreach (var feedback in report.Feedbacks)
        {
            sb.AppendLine($"### {feedback.Checkpoint} ({feedback.Timestamp:HH:mm:ss})");
            sb.AppendLine($"- **URL:** {feedback.Url}");
            sb.AppendLine($"- **Title:** {feedback.Title}");
            sb.AppendLine($"- **Screenshot:** `{feedback.ScreenshotPath}`");
            
            if (feedback.StyleIssues.Any())
            {
                sb.AppendLine($"- **Style Issues:** {feedback.StyleIssues.Count}");
                foreach (var issue in feedback.StyleIssues.Take(3))
                {
                    sb.AppendLine($"  - {issue.Element}: {issue.Issue}");
                }
            }
            sb.AppendLine();
        }

        if (report.ConsoleErrors.Any())
        {
            sb.AppendLine("## Console Errors");
            sb.AppendLine("```");
            foreach (var error in report.ConsoleErrors.Take(10))
            {
                sb.AppendLine($"[{error.Timestamp:HH:mm:ss}] {error.Type}: {error.Message}");
            }
            sb.AppendLine("```");
            sb.AppendLine();
        }

        sb.AppendLine("## Recommendations for Fixes");
        sb.AppendLine();
        sb.AppendLine("Based on the issues detected, here are the recommended fixes:");
        sb.AppendLine();

        // Generate recommendations based on issues
        var recommendations = GenerateRecommendations(report.UIIssues, report.ConsoleErrors);
        foreach (var rec in recommendations)
        {
            sb.AppendLine($"1. **{rec.Title}**");
            sb.AppendLine($"   - {rec.Description}");
            sb.AppendLine($"   - File: `{rec.FilePath}`");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private List<FixRecommendation> GenerateRecommendations(List<UIIssue> uiIssues, List<ConsoleError> consoleErrors)
    {
        var recommendations = new List<FixRecommendation>();

        // Analyze UI issues
        if (uiIssues.Any(i => i.Message.Contains("horizontal scrollbar")))
        {
            recommendations.Add(new FixRecommendation
            {
                Title = "Fix Layout Overflow",
                Description = "Add CSS to prevent horizontal overflow: `overflow-x: hidden` on body or check for fixed-width elements",
                FilePath = "Likely in MainLayout.razor.css or global styles"
            });
        }

        if (uiIssues.Any(i => i.Message.Contains("overlapping")))
        {
            recommendations.Add(new FixRecommendation
            {
                Title = "Fix Overlapping Elements",
                Description = "Check z-index values and positioning. Consider using CSS Grid or Flexbox for layout",
                FilePath = "Check component-specific CSS files"
            });
        }

        if (uiIssues.Any(i => i.Message.Contains("broken images")))
        {
            recommendations.Add(new FixRecommendation
            {
                Title = "Fix Image Loading",
                Description = "Check image src paths and ensure images exist. Add proper error handling for missing images",
                FilePath = "Components that display images"
            });
        }

        if (uiIssues.Any(i => i.Message.Contains("low contrast")))
        {
            recommendations.Add(new FixRecommendation
            {
                Title = "Improve Color Contrast",
                Description = "Adjust text and background colors to meet WCAG AA standards (4.5:1 ratio minimum)",
                FilePath = "Global CSS or component stylesheets"
            });
        }

        // Analyze console errors
        foreach (var error in consoleErrors.GroupBy(e => e.Type))
        {
            if (error.Key.Contains("TypeError") || error.Key.Contains("ReferenceError"))
            {
                recommendations.Add(new FixRecommendation
                {
                    Title = $"Fix JavaScript Error: {error.Key}",
                    Description = $"Found {error.Count()} {error.Key} errors. Review JavaScript code for null references or undefined variables",
                    FilePath = "Check browser console for stack traces"
                });
            }
        }

        return recommendations;
    }

    // Event handlers for capturing runtime information
    private readonly List<ConsoleError> _consoleErrors = new();
    private readonly List<NetworkError> _networkErrors = new();

    private void OnConsoleMessage(object? sender, IConsoleMessage message)
    {
        if (message.Type == "error" || message.Type == "warning")
        {
            _consoleErrors.Add(new ConsoleError
            {
                Type = message.Type,
                Message = message.Text,
                Timestamp = DateTime.Now
            });
            
            LogError($"Console {message.Type}: {message.Text}");
        }
    }

    private void OnPageError(object? sender, string error)
    {
        _consoleErrors.Add(new ConsoleError
        {
            Type = "page-error",
            Message = error,
            Timestamp = DateTime.Now
        });
        
        LogError($"Page error: {error}");
    }

    private void OnRequest(object? sender, IRequest request)
    {
        // Log all requests for analysis
    }

    private void OnResponse(object? sender, IResponse response)
    {
        if (response.Status >= 400)
        {
            _networkErrors.Add(new NetworkError
            {
                Url = response.Url,
                Status = response.Status,
                Method = response.Request.Method,
                Timestamp = DateTime.Now
            });
            
            LogError($"Network error: {response.Status} {response.Url}");
        }
    }

    private void LogInfo(string message)
    {
        var logPath = Path.Combine(_logsDir, "test.log");
        File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] INFO: {message}\n");
    }

    private void LogError(string message)
    {
        var logPath = Path.Combine(_logsDir, "test.log");
        File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] ERROR: {message}\n");
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
    }

    public void Dispose()
    {
        _page.Console -= OnConsoleMessage;
        _page.PageError -= OnPageError;
        _page.Request -= OnRequest;
        _page.Response -= OnResponse;
    }
}

public class TestFeedback
{
    public string Checkpoint { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = "";
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public string ScreenshotPath { get; set; } = "";
    public string DomSnapshot { get; set; } = "";
    public string VisibleText { get; set; } = "";
    public List<StyleIssue> StyleIssues { get; set; } = new();
    public string AccessibilitySnapshot { get; set; } = "";
}

public class StyleIssue
{
    public string Element { get; set; } = "";
    public string Issue { get; set; } = "";
    public string Severity { get; set; } = "";
}

public class UIIssue
{
    public string Type { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Message { get; set; } = "";
    public string Screenshot { get; set; } = "";
}

public class ConsoleError
{
    public string Type { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

public class NetworkError
{
    public string Url { get; set; } = "";
    public int Status { get; set; }
    public string Method { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

public class TestReport
{
    public string TestName { get; set; } = "";
    public bool Passed { get; set; }
    public string? Error { get; set; }
    public List<TestFeedback> Feedbacks { get; set; } = new();
    public List<UIIssue> UIIssues { get; set; } = new();
    public List<ConsoleError> ConsoleErrors { get; set; } = new();
    public List<NetworkError> NetworkErrors { get; set; } = new();
    public TimeSpan Duration { get; set; }
}

public class FixRecommendation
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string FilePath { get; set; } = "";
}

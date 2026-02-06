using System.Diagnostics;

namespace BlazorBook.E2E.Infrastructure;

/// <summary>
/// Factory that starts and manages the BlazorBook.Web application for E2E testing.
/// </summary>
public class BlazorBookWebFactory : IDisposable
{
    private Process? _process;
    private bool _isDisposed;
    
    /// <summary>
    /// The base URL where the application is running.
    /// </summary>
    public string BaseUrl { get; private set; } = "http://localhost:5555";
    
    /// <summary>
    /// The port number the application is running on.
    /// </summary>
    public int Port { get; private set; } = 5555;

    /// <summary>
    /// Starts the BlazorBook.Web application.
    /// </summary>
    public async Task StartAsync()
    {
        // Find the project path relative to test assembly
        var projectPath = FindProjectPath();
        
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" --urls \"{BaseUrl}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        _process = Process.Start(startInfo);
        
        if (_process == null)
        {
            throw new InvalidOperationException("Failed to start BlazorBook.Web process");
        }

        // Wait for the application to be ready
        await WaitForApplicationReadyAsync();
    }

    private string FindProjectPath()
    {
        // Navigate from test bin folder to project
        var currentDir = Directory.GetCurrentDirectory();
        var solutionDir = currentDir;
        
        // Walk up to find the solution root (contains ActivityStream.slnx)
        while (!File.Exists(Path.Combine(solutionDir, "ActivityStream.slnx")) && 
               Directory.GetParent(solutionDir) != null)
        {
            solutionDir = Directory.GetParent(solutionDir)!.FullName;
        }
        
        var projectPath = Path.Combine(solutionDir, "src", "BlazorBook.Web", "BlazorBook.Web.csproj");
        
        if (!File.Exists(projectPath))
        {
            throw new FileNotFoundException($"Could not find BlazorBook.Web.csproj at {projectPath}");
        }
        
        return projectPath;
    }

    private async Task WaitForApplicationReadyAsync(int timeoutSeconds = 60)
    {
        using var httpClient = new HttpClient();
        var startTime = DateTime.UtcNow;
        
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(timeoutSeconds))
        {
            try
            {
                var response = await httpClient.GetAsync(BaseUrl);
                if (response.IsSuccessStatusCode)
                {
                    // Give it a bit more time to fully initialize
                    await Task.Delay(1000);
                    return;
                }
            }
            catch
            {
                // Application not ready yet
            }
            
            await Task.Delay(500);
        }
        
        throw new TimeoutException($"BlazorBook.Web did not start within {timeoutSeconds} seconds");
    }

    /// <summary>
    /// Stops the BlazorBook.Web application.
    /// </summary>
    public void Stop()
    {
        if (_process != null && !_process.HasExited)
        {
            try
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(5000);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            Stop();
            _process?.Dispose();
            _isDisposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

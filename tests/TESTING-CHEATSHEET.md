# 🚀 Quick Test Commands Cheat Sheet

## Common Commands

```powershell
# Run all tests (server must be running)
.\Run-ComprehensiveTests.ps1

# Run tests and auto-start server
.\Run-ComprehensiveTests.ps1 -StartServer

# Run with visible browser (debugging)
.\Run-ComprehensiveTests.ps1 -HeadlessMode:$false

# Run specific category
.\Run-ComprehensiveTests.ps1 -Category "UI"
.\Run-ComprehensiveTests.ps1 -Category "Functionality"
.\Run-ComprehensiveTests.ps1 -Category "Navigation"

# Skip report generation
.\Run-ComprehensiveTests.ps1 -GenerateReport $false -OpenReport $false

# Custom server URL
.\Run-ComprehensiveTests.ps1 -ServerUrl "http://localhost:8080"
```

## Test Categories

- `UI` - User interface and layout tests
- `Functionality` - Feature behavior tests
- `Navigation` - Routing and navigation tests
- `Authentication` - Login and signup
- `Feed` - Feed and posts
- `Profile` - User profiles
- `Messages` - Messaging features
- `Friends` - Friend management
- `Notifications` - Notification system
- `Search` - Search functionality
- `Responsive` - Mobile/tablet/desktop layouts

## Direct dotnet test Commands

```powershell
# Run from test directory
cd tests/BlazorBook.E2E

# Run all tests
dotnet test

# Run with filter
dotnet test --filter "TestCategory=UI"

# Run single test
dotnet test --filter "FullyQualifiedName~MyTestName"

# Verbose output
dotnet test --logger "console;verbosity=detailed"
```

## Setup Commands

```powershell
# First-time setup
cd tests/BlazorBook.E2E
dotnet build
pwsh bin/Debug/net8.0/playwright.ps1 install

# Install specific browser
pwsh bin/Debug/net8.0/playwright.ps1 install chromium

# Update Playwright
dotnet add package Microsoft.Playwright --version <latest>
pwsh bin/Debug/net8.0/playwright.ps1 install
```

## Debugging

```powershell
# Playwright inspector
$env:PWDEBUG=1
dotnet test tests/BlazorBook.E2E

# Slow motion (milliseconds)
$env:SLOW_MO=1000
dotnet test tests/BlazorBook.E2E

# Keep browser open on failure
$env:HEADED=1
dotnet test tests/BlazorBook.E2E
```

## Clean Up

```powershell
# Remove old results
Remove-Item tests/BlazorBook.E2E/TestResults/* -Recurse -Force

# Remove screenshots
Remove-Item tests/BlazorBook.E2E/screenshots/* -Recurse -Force

# Full clean
Remove-Item tests/BlazorBook.E2E/bin -Recurse -Force
Remove-Item tests/BlazorBook.E2E/obj -Recurse -Force
```

## Typical Workflow

```powershell
# 1. Start the app (Terminal 1)
cd src/BlazorBook.Web
dotnet run --urls http://localhost:5555

# 2. Run tests (Terminal 2)
cd tests
.\Run-ComprehensiveTests.ps1

# 3. View results
# Report opens automatically in browser
# Or navigate to tests/BlazorBook.E2E/TestResults/
```

## File Locations

- **Test Files**: `tests/BlazorBook.E2E/Tests/`
- **Screenshots**: `tests/BlazorBook.E2E/screenshots/`
- **Reports**: `tests/BlazorBook.E2E/TestResults/`
- **Infrastructure**: `tests/BlazorBook.E2E/Infrastructure/`

## Environment Variables

```powershell
# Set server URL
$env:BLAZORBOOK_URL = "http://localhost:5555"

# Enable headed mode
$env:HEADED = "1"

# Enable debug mode
$env:PWDEBUG = "1"

# Slow motion (ms)
$env:SLOW_MO = "500"
```

## CI/CD Quick Setup

```yaml
- name: Run E2E Tests
  run: |
    cd tests
    .\Run-ComprehensiveTests.ps1 -StartServer -HeadlessMode:$true
```

---

📖 **Full Documentation**: See [README-TESTING.md](README-TESTING.md)

# BlazorBook.Web - Quick Setup Script
# Run this script from the repository root

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "BlazorBook.Web - EF Core + REST API Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Restore packages
Write-Host "[1/5] Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore src/BlazorBook.Web/BlazorBook.Web.csproj
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Package restore failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Packages restored" -ForegroundColor Green
Write-Host ""

# Step 2: Build project
Write-Host "[2/5] Building project..." -ForegroundColor Yellow
dotnet build src/BlazorBook.Web/BlazorBook.Web.csproj --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Build failed! Check errors above." -ForegroundColor Red
    exit 1
}
Write-Host "✓ Build successful" -ForegroundColor Green
Write-Host ""

# Step 3: Generate EF Core migration
Write-Host "[3/5] Generating EF Core migration..." -ForegroundColor Yellow
dotnet ef migrations add InitialCreate --project src/BlazorBook.Web --output-dir Data/Migrations
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Migration generation failed!" -ForegroundColor Red
    Write-Host "Note: This might be expected if migrations already exist." -ForegroundColor Yellow
} else {
    Write-Host "✓ Migration generated" -ForegroundColor Green
}
Write-Host ""

# Step 4: Apply migrations (create database)
Write-Host "[4/5] Applying migrations to database..." -ForegroundColor Yellow
dotnet ef database update --project src/BlazorBook.Web
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Database update failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Database created/updated" -ForegroundColor Green
Write-Host ""

# Step 5: Show setup summary
Write-Host "[5/5] Setup Summary" -ForegroundColor Yellow
Write-Host "==================" -ForegroundColor Yellow
Write-Host ""

if (Test-Path "blazorbook.db") {
    $dbSize = (Get-Item "blazorbook.db").Length
    Write-Host "✓ Database file: blazorbook.db ($dbSize bytes)" -ForegroundColor Green
} else {
    Write-Host "⚠ Database file not found (might be in src/BlazorBook.Web/)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Review configuration in src/BlazorBook.Web/appsettings.json"
Write-Host "2. Set Storage:Mode to 'EFCore' to use database (currently set for InMemory)"
Write-Host "3. Run the application: cd src/BlazorBook.Web && dotnet run"
Write-Host "4. Access Blazor UI at: https://localhost:5001"
Write-Host "5. Access Swagger API at: https://localhost:5001/swagger"
Write-Host ""
Write-Host "For detailed implementation plan, see:" -ForegroundColor Cyan
Write-Host "  EFCORE_REST_API_IMPLEMENTATION_PLAN.md" -ForegroundColor White
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Setup complete! ✓" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan

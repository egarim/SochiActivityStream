# Media Upload Fix - MediaValidationException Resolution

## Problem
When attempting to create a post with an image in the BlazorBook application, a `MediaValidationException` was thrown with the error: "Upload not confirmed - blob does not exist in storage."

## Root Cause
The issue was caused by a mismatch between where files were being stored and where the validation logic was checking for them:

1. **LocalMediaUploadService** was saving files **only to the local filesystem** (`wwwroot/uploads/`)
2. **MediaService.ConfirmUploadAsync** was checking for blob existence in **Azure Blob Storage** (via `AzureBlobStorageProvider` / Azurite)
3. Since the file existed locally but NOT in Azure/Azurite, the validation check failed

## Solution
Modified `LocalMediaUploadService` to upload files to **both** locations:
- Local filesystem (for serving via static files)  
- Storage provider (for validation by MediaService)

### Changes Made

#### 1. Updated `LocalMediaUploadService.cs`
**File**: `src/BlazorBook.Web/Services/LocalMediaUploadService.cs`

**Changes**:
- Added `IMediaStorageProvider` and `ILogger` dependency injection
- Added call to `_storageProvider.UploadBytesAsync()` before confirming upload
- Added logging to track upload progress and diagnose issues
- Added error handling with try-catch blocks
- Updated constructor to accept the storage provider and logger

```csharp
// Added fields
private readonly IMediaStorageProvider _storageProvider;
private readonly ILogger<LocalMediaUploadService> _logger;

// Updated constructor
public LocalMediaUploadService(
    IMediaService mediaService,
    IMediaStorageProvider storageProvider,  // NEW
    IWebHostEnvironment environment,
    ICurrentUserService currentUser,
    ILogger<LocalMediaUploadService> logger)  // NEW
{
    _mediaService = mediaService;
    _storageProvider = storageProvider;  // NEW
    _environment = environment;
    _currentUser = currentUser;
    _logger = logger;  // NEW
}

// Added upload to storage provider with logging
await File.WriteAllBytesAsync(fullPath, data, ct);
_logger.LogInformation("📁 Saved file locally: {FullPath}", fullPath);

// Upload to storage provider so ConfirmUploadAsync can verify the blob exists
try
{
    _logger.LogInformation("☁️ Uploading to storage provider: {BlobPath} ({Size} bytes)", blobPath, data.Length);
    await _storageProvider.UploadBytesAsync(blobPath, data, contentType, ct);
    _logger.LogInformation("✅ Successfully uploaded to storage provider: {BlobPath}", blobPath);
    
    // Verify the blob exists
    var exists = await _storageProvider.ExistsAsync(blobPath, ct);
    _logger.LogInformation("🔍 Blob exists check: {BlobPath} = {Exists}", blobPath, exists);
}
catch (Exception ex)
{
    _logger.LogError(ex, "❌ Failed to upload to storage provider: {BlobPath}", blobPath);
    throw;
}

// Confirm upload
_logger.LogInformation("📝 Confirming upload: MediaId={MediaId}", urlResult.MediaId);
var media = await _mediaService.ConfirmUploadAsync(TenantId, urlResult.MediaId, null, ct);
_logger.LogInformation("✅ Upload confirmed: MediaId={MediaId}", media.Id);
```

#### 2. Updated `Program.cs`
**File**: `src/BlazorBook.Web/Program.cs`

**Changes**:
- Modified service registration to inject `IMediaStorageProvider` and `ILogger`

```csharp
// Changed from:
builder.Services.AddScoped<IMediaUploadService, LocalMediaUploadService>();

// To:
builder.Services.AddScoped<IMediaUploadService>(sp =>
{
    var mediaService = sp.GetRequiredService<IMediaService>();
    var storageProvider = sp.GetRequiredService<IMediaStorageProvider>();
    var environment = sp.GetRequiredService<IWebHostEnvironment>();
    var currentUser = sp.GetRequiredService<ICurrentUserService>();
    var logger = sp.GetRequiredService<ILogger<LocalMediaUploadService>>();
    return new LocalMediaUploadService(mediaService, storageProvider, environment, currentUser, logger);
});
```

## Prerequisites 
**IMPORTANT**: Azurite (Azure Storage Emulator) must be running for media uploads to work.

### Starting Azurite
```powershell
# Start Azurite in the background
azurite --silent --location c:\azurite --debug c:\azurite\debug.log
```

If port 10000 is already in use, Azurite is likely already running. You can verify with:
```powershell
Get-NetTCPConnection -LocalPort 10000 -ErrorAction SilentlyContinue
```

## Validation Flow
After the fix, the complete media upload flow is:

1. **Request Upload URL**: `MediaService.RequestUploadUrlAsync()` creates a pending media record
2. **Upload to Filesystem**: File is saved to `wwwroot/uploads/{blobPath}`
3. **Upload to Storage Provider**: File is uploaded to Azure/Azurite via `UploadBytesAsync()`
4. **Verify Blob Exists**: Checks that the blob was successfully uploaded
5. **Confirm Upload**: `MediaService.ConfirmUploadAsync()` validates:
   - Media record exists
   - Status is `Pending`
   - Blob exists in storage (now passes! ✅)
   - Updates metadata and marks as confirmed

## Manual Testing
To test the fix manually:

1. **Start Azurite** (if not already running):
   ```powershell
   azurite --silent --location c:\azurite
   ```

2. **Start the application**:
   ```powershell
   dotnet run --project src/BlazorBook.Web/BlazorBook.Web.csproj --urls http://localhost:5555
   ```

3. **Test in browser**:
   - Navigate to http://localhost:5555
   - Log in as a user (e.g., Alice, Bob, or Charlie)
   - Click the "Photo/Video" button in the post composer
   - Select an image file
   - Add some text to your post
   - Click "Post"
   - Verify the post appears with the image

4. **Check logs** for successful upload:
   Look for these log messages in the console:
   - 📁 Saved file locally
   - ☁️ Uploading to storage provider
   - ✅ Successfully uploaded to storage provider
   - 🔍 Blob exists check: true
   - 📝 Confirming upload
   - ✅ Upload confirmed

## E2E Testing Challenges
An E2E test was created in [FeedTests.cs](tests/BlazorBook.E2E/Tests/FeedTests.cs) (`CreatePost_WithImage_UploadsSuccessfully`), but it currently fails due to DevExpress FileInput component compatibility issues with Playwright:

- The DevExpress `DxFileInput` component doesn't use a native file chooser
- Playwright's `SetInputFilesAsync` and `RunAndWaitForFileChooserAsync` don't trigger the component's internal event handlers
- The `FilesUploading` event isn't fired, so the UI doesn't recognize the file selection

### Workarounds for Automated Testing
1. **Manual testing** (recommended for now)
2. **Integration tests** that bypass the UI and test the upload service directly
3. **Custom DevExpress testing** using their recommended approach

## Impact
- ✅ Posts with images can now be created successfully (when Azurite is running)
- ✅ Files are stored in both local filesystem and Azure/Azurite
- ✅ MediaService validation passes correctly  
- ✅ Comprehensive logging for debugging
- ✅ No breaking changes to existing functionality
- ⚠️ Requires Azurite to be running
- ⚠️ E2E tests need alternative approach for DevExpress components

## Troubleshooting

### Error: "UploadNotConfirmed"
- **Cause**: Azurite is not running or not accessible
- **Solution**: Start Azurite and verify it's listening on port 10000

### Error: Connection refused
- **Cause**: Azurite connection string is incorrect
- **Solution**: Verify connection string in appsettings is `UseDevelopmentStorage=true`

### Files not appearing after upload
- **Check logs**: Look for the emoji-prefixed log messages
- **Verify Azurite**: Ensure Azurite is running and accessible
- **Check permissions**: Ensure the app has write access to `wwwroot/uploads/`

## Date
February 9, 2026

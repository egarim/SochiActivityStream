using ActivityStream.Abstractions;
using ActivityStream.Core;
using ActivityStream.Store.InMemory;
using Chat.Abstractions;
using Chat.Core;
using Chat.Store.InMemory;
using Content.Abstractions;
using Content.Core;
using Content.Store.InMemory;
using DevExpress.Blazor;
using MudBlazor.Services;
using Identity.Abstractions;
using Identity.Core;
using Identity.Store.InMemory;
using Inbox.Abstractions;
using Inbox.Core;
using Inbox.Store.InMemory;
using Media.Abstractions;
using Media.Core;
using Media.Store.InMemory;
using RelationshipService.Abstractions;
using RelationshipService.Core;
using RelationshipService.Store.InMemory;
using Search.Abstractions;
using Search.Core;
using Search.Index.InMemory;

using Sochi.Navigation.Extensions;

using SocialKit.Components.Abstractions;
using SocialKit.Components.Extensions;

using BlazorBook.Web;
using BlazorBook.Web.Components;
using BlazorBook.Web.Services;
using BlazorBook.Web.Data;
using BlazorBook.Web.Stores.EFCore;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

using Serilog;
using Serilog.Events;

// ═══════════════════════════════════════════════════════════════════════════════
// SERILOG CONFIGURATION
// ═══════════════════════════════════════════════════════════════════════════════
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/blazorbook-.txt", 
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting BlazorBook.Web application");

var builder = WebApplication.CreateBuilder(args);

// Use Serilog for logging
builder.Host.UseSerilog();

// ═══════════════════════════════════════════════════════════════════════════════
// DATABASE CONTEXT (EF Core + SQLite)
// ═══════════════════════════════════════════════════════════════════════════════
var storageMode = builder.Configuration["Storage:Mode"] ?? "InMemory";
if (storageMode == "EFCore")
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=blazorbook.db";
    
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString), ServiceLifetime.Scoped);
}

// ═══════════════════════════════════════════════════════════════════════════════
// JWT AUTHENTICATION
// ═══════════════════════════════════════════════════════════════════════════════
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "default-secret-key-min-32-chars-long";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BlazorBook.Web";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "BlazorBook.Api";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
builder.Services.AddScoped<JwtTokenService>();

// ═══════════════════════════════════════════════════════════════════════════════
// REST API CONTROLLERS + SWAGGER
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BlazorBook API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ═══════════════════════════════════════════════════════════════════════════════
// BLAZOR SERVICES
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// MudBlazor
builder.Services.AddMudServices();

// DevExpress Blazor
builder.Services.AddDevExpressBlazor();

// ═══════════════════════════════════════════════════════════════════════════════
// SOCHI.NAVIGATION (MVVM)
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddSochiNavigation();

// ═══════════════════════════════════════════════════════════════════════════════
// IDENTITY SERVICE
// ═══════════════════════════════════════════════════════════════════════════════
if (storageMode == "EFCore")
{
    builder.Services.AddScoped<IUserStore, EFCoreUserStore>();
    builder.Services.AddScoped<IProfileStore, EFCoreProfileStore>();
    builder.Services.AddScoped<IMembershipStore, EFCoreMembershipStore>();
    builder.Services.AddScoped<ISessionStore, InMemorySessionStore>();
}
else
{
    builder.Services.AddScoped<IUserStore, InMemoryUserStore>();
    builder.Services.AddScoped<IProfileStore, InMemoryProfileStore>();
    builder.Services.AddScoped<IMembershipStore, InMemoryMembershipStore>();
    builder.Services.AddScoped<ISessionStore, InMemorySessionStore>();
}
builder.Services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddScoped<Identity.Abstractions.IIdGenerator, Identity.Core.UlidIdGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProfileService, ProfileService>();

// ═══════════════════════════════════════════════════════════════════════════════
// CONTENT SERVICE (Posts, Comments, Reactions)
// ═══════════════════════════════════════════════════════════════════════════════
if (storageMode == "EFCore")
{
    builder.Services.AddScoped<IPostStore, EFCorePostStore>();
    builder.Services.AddScoped<ICommentStore, EFCoreCommentStore>();
    builder.Services.AddScoped<IReactionStore, EFCoreReactionStore>();
}
else
{
    builder.Services.AddScoped<IPostStore, InMemoryPostStore>();
    builder.Services.AddScoped<ICommentStore, InMemoryCommentStore>();
    builder.Services.AddScoped<IReactionStore, InMemoryReactionStore>();
}
builder.Services.AddScoped<Content.Abstractions.IIdGenerator, Content.Core.UlidIdGenerator>();
builder.Services.AddScoped<IContentService, ContentService>();

// ═══════════════════════════════════════════════════════════════════════════════
// CHAT SERVICE
// ═══════════════════════════════════════════════════════════════════════════════
if (storageMode == "EFCore")
{
    builder.Services.AddScoped<IConversationStore, EFCoreConversationStore>();
    builder.Services.AddScoped<IMessageStore, EFCoreMessageStore>();
}
else
{
    builder.Services.AddScoped<IConversationStore, InMemoryConversationStore>();
    builder.Services.AddScoped<IMessageStore, InMemoryMessageStore>();
}
builder.Services.AddScoped<IChatNotifier, NullChatNotifier>();
builder.Services.AddScoped<Chat.Abstractions.IIdGenerator, Chat.Core.UlidIdGenerator>();
builder.Services.AddScoped<IChatService, ChatService>();

// ═══════════════════════════════════════════════════════════════════════════════
// MEDIA SERVICE
// ═══════════════════════════════════════════════════════════════════════════════
if (storageMode == "EFCore")
{
    builder.Services.AddScoped<IMediaStore, EFCoreMediaStore>();
}
else
{
    builder.Services.AddScoped<IMediaStore, InMemoryMediaStore>();
}
builder.Services.AddSingleton<IMediaStorageProvider>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("AzureStorage") ?? "UseDevelopmentStorage=true";
    return new Media.Core.AzureBlobStorageProvider(new Media.Core.AzureBlobStorageOptions
    {
        ConnectionString = connectionString,
        ContainerName = "blazorbook-media",
        CreateContainerIfNotExists = true
    });
});
builder.Services.AddScoped<ActivityStream.Abstractions.IIdGenerator, ActivityStream.Core.UlidIdGenerator>();
builder.Services.AddScoped<IMediaService, MediaService>();

// ═══════════════════════════════════════════════════════════════════════════════
// ACTIVITY STREAM SERVICE
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddScoped<IActivityStore, InMemoryActivityStore>();
builder.Services.AddScoped<IActivityValidator, DefaultActivityValidator>();
builder.Services.AddScoped<IActivityStreamService, ActivityStreamService>();

// ═══════════════════════════════════════════════════════════════════════════════
// RELATIONSHIP SERVICE
// ═══════════════════════════════════════════════════════════════════════════════
if (storageMode == "EFCore")
{
    builder.Services.AddScoped<IRelationshipStore, EFCoreRelationshipStore>();
}
else
{
    builder.Services.AddScoped<IRelationshipStore, InMemoryRelationshipStore>();
}
builder.Services.AddScoped<IRelationshipService, RelationshipServiceImpl>();

// ═══════════════════════════════════════════════════════════════════════════════
// INBOX SERVICE (Notifications)
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddScoped<IInboxStore, InMemoryInboxStore>();
builder.Services.AddScoped<IFollowRequestStore, InMemoryFollowRequestStore>();
builder.Services.AddScoped<IRecipientExpansionPolicy, DefaultRecipientExpansionPolicy>();
builder.Services.AddSingleton<IEntityGovernancePolicy, AllowAllGovernancePolicy>();
builder.Services.AddScoped<IInboxNotificationService, InboxNotificationService>();

// Note: Realtime transport remains disabled for this demo app.

// ═══════════════════════════════════════════════════════════════════════════════
// SEARCH SERVICE
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddScoped<ITextAnalyzer, SimpleTextAnalyzer>();
if (storageMode == "EFCore")
{
    builder.Services.AddScoped<ISearchIndex, EFCoreSearchIndex>();
    // Register interfaces that ISearchIndex implements
    builder.Services.AddScoped<ISearchService>(sp => sp.GetRequiredService<ISearchIndex>());
    builder.Services.AddScoped<ISearchIndexer>(sp => sp.GetRequiredService<ISearchIndex>());
}
else
{
    builder.Services.AddScoped<ISearchIndex, InMemorySearchIndex>();
    // Register interfaces that ISearchIndex implements
    builder.Services.AddScoped<ISearchService>(sp => sp.GetRequiredService<ISearchIndex>());
    builder.Services.AddScoped<ISearchIndexer>(sp => sp.GetRequiredService<ISearchIndex>());
}
// Note: SearchValidator is a static class, no DI registration needed

// ═══════════════════════════════════════════════════════════════════════════════
// APPLICATION SERVICES
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<DemoDataSeeder>();

// ═══════════════════════════════════════════════════════════════════════════════
// SOCIALKIT (FeedService, ViewModels, etc.)
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddSocialKit();
builder.Services.AddScoped<IMediaUploadService, LocalMediaUploadService>();

var app = builder.Build();

// ═══════════════════════════════════════════════════════════════════════════════
// DATABASE MIGRATION AND SEEDING
// ═══════════════════════════════════════════════════════════════════════════════
if (storageMode == "EFCore" && app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
        
        var seedDemo = builder.Configuration.GetValue<bool>("Database:SeedDemo");
        if (seedDemo)
        {
            var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
            await seeder.SeedAsync();
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BlazorBook API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// ═══════════════════════════════════════════════════════════════════════════════
// GOVERNANCE POLICY (Demo Implementation)
// ═══════════════════════════════════════════════════════════════════════════════
public class AllowAllGovernancePolicy : IEntityGovernancePolicy
{
    public Task<bool> IsTargetableAsync(
        string tenantId,
        ActivityStream.Abstractions.EntityRefDto entity,
        CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<bool> RequiresApprovalToFollowAsync(
        string tenantId,
        ActivityStream.Abstractions.EntityRefDto requester,
        ActivityStream.Abstractions.EntityRefDto target,
        RelationshipKind requestedKind,
        CancellationToken ct = default)
        => Task.FromResult(false);

    public Task<IReadOnlyList<ActivityStream.Abstractions.EntityRefDto>> GetApproversAsync(
        string tenantId,
        ActivityStream.Abstractions.EntityRefDto target,
        CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ActivityStream.Abstractions.EntityRefDto>>(Array.Empty<ActivityStream.Abstractions.EntityRefDto>());
}

// ═══════════════════════════════════════════════════════════════════════════════
// MOCK STORAGE PROVIDER (LOCAL FILE STORAGE)
// ═══════════════════════════════════════════════════════════════════════════════
public class MockStorageProvider : IMediaStorageProvider
{
    public Task<string> GenerateUploadUrlAsync(string blobPath, string contentType, long maxSizeBytes, TimeSpan expiry, CancellationToken ct = default)
        => Task.FromResult($"/uploads/{blobPath}");
    
    public Task<string> GenerateDownloadUrlAsync(string blobPath, TimeSpan expiry, CancellationToken ct = default)
        => Task.FromResult($"/uploads/{blobPath}");
    
    public Task<bool> ExistsAsync(string blobPath, CancellationToken ct = default)
        => Task.FromResult(true);
    
    public Task<BlobProperties?> GetPropertiesAsync(string blobPath, CancellationToken ct = default)
        => Task.FromResult<BlobProperties?>(new BlobProperties { SizeBytes = 1024, ContentType = "image/png" });
    
    public Task DeleteAsync(string blobPath, CancellationToken ct = default)
        => Task.CompletedTask;
    
    public Task CopyAsync(string sourcePath, string destPath, CancellationToken ct = default)
        => Task.CompletedTask;
    
    public Task UploadBytesAsync(string blobPath, byte[] data, string contentType, CancellationToken ct = default)
    {
        // Mock implementation - in real app would save to disk or blob storage
        Console.WriteLine($"[MockStorageProvider] Upload: {blobPath} ({data.Length} bytes, {contentType})");
        return Task.CompletedTask;
    }
}

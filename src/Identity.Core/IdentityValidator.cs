using System.Text.RegularExpressions;
using Identity.Abstractions;

namespace Identity.Core;

/// <summary>
/// Static methods for validating identity values.
/// </summary>
public static partial class IdentityValidator
{
    /// <summary>Maximum email length.</summary>
    public const int MaxEmailLength = 254;

    /// <summary>Minimum username length.</summary>
    public const int MinUsernameLength = 3;

    /// <summary>Maximum username length.</summary>
    public const int MaxUsernameLength = 50;

    /// <summary>Minimum handle length.</summary>
    public const int MinHandleLength = 3;

    /// <summary>Maximum handle length.</summary>
    public const int MaxHandleLength = 50;

    /// <summary>Minimum password length.</summary>
    public const int MinPasswordLength = 8;

    /// <summary>Maximum password length.</summary>
    public const int MaxPasswordLength = 256;

    /// <summary>Maximum display name length.</summary>
    public const int MaxDisplayNameLength = 100;

    /// <summary>Maximum tenant ID length.</summary>
    public const int MaxTenantIdLength = 100;

    // Username/handle: alphanumeric + underscore
    [GeneratedRegex(@"^[a-z0-9_]+$", RegexOptions.Compiled)]
    private static partial Regex UsernameHandlePattern();

    // Basic email pattern
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex EmailPattern();

    /// <summary>
    /// Validates a sign-up request.
    /// </summary>
    public static IReadOnlyList<IdentityValidationError> ValidateSignUp(SignUpRequest request, string tenantId)
    {
        var errors = new List<IdentityValidationError>();

        ValidateTenantId(tenantId, errors);
        ValidateEmail(request.Email, errors);
        ValidateUsername(request.Username, errors);
        ValidatePassword(request.Password, errors);
        ValidateDisplayName(request.DisplayName, "DisplayName", errors);

        return errors;
    }

    /// <summary>
    /// Validates a sign-in request.
    /// </summary>
    public static IReadOnlyList<IdentityValidationError> ValidateSignIn(SignInRequest request, string tenantId)
    {
        var errors = new List<IdentityValidationError>();

        ValidateTenantId(tenantId, errors);

        if (string.IsNullOrWhiteSpace(request.Login))
            errors.Add(new("REQUIRED", "Login is required.", "Login"));

        if (string.IsNullOrWhiteSpace(request.Password))
            errors.Add(new("REQUIRED", "Password is required.", "Password"));

        return errors;
    }

    /// <summary>
    /// Validates a create profile request.
    /// </summary>
    public static IReadOnlyList<IdentityValidationError> ValidateCreateProfile(CreateProfileRequest request, string tenantId, string userId)
    {
        var errors = new List<IdentityValidationError>();

        ValidateTenantId(tenantId, errors);

        if (string.IsNullOrWhiteSpace(userId))
            errors.Add(new("REQUIRED", "UserId is required.", "UserId"));

        ValidateHandle(request.Handle, errors);
        ValidateDisplayName(request.DisplayName, "DisplayName", errors);

        return errors;
    }

    /// <summary>
    /// Validates an add member request.
    /// </summary>
    public static IReadOnlyList<IdentityValidationError> ValidateAddMember(AddMemberRequest request, string tenantId, string profileId)
    {
        var errors = new List<IdentityValidationError>();

        ValidateTenantId(tenantId, errors);

        if (string.IsNullOrWhiteSpace(profileId))
            errors.Add(new("REQUIRED", "ProfileId is required.", "ProfileId"));

        if (string.IsNullOrWhiteSpace(request.UserId))
            errors.Add(new("REQUIRED", "UserId is required.", "UserId"));

        return errors;
    }

    /// <summary>
    /// Validates an invite member request.
    /// </summary>
    public static IReadOnlyList<IdentityValidationError> ValidateInviteMember(InviteMemberRequest request, string tenantId, string profileId)
    {
        var errors = new List<IdentityValidationError>();

        ValidateTenantId(tenantId, errors);

        if (string.IsNullOrWhiteSpace(profileId))
            errors.Add(new("REQUIRED", "ProfileId is required.", "ProfileId"));

        if (string.IsNullOrWhiteSpace(request.Login))
            errors.Add(new("REQUIRED", "Login is required.", "Login"));

        return errors;
    }

    private static void ValidateTenantId(string? tenantId, List<IdentityValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            errors.Add(new("REQUIRED", "TenantId is required.", "TenantId"));
            return;
        }

        if (tenantId.Length > MaxTenantIdLength)
            errors.Add(new("MAX_LENGTH", $"TenantId must be at most {MaxTenantIdLength} characters.", "TenantId"));
    }

    private static void ValidateEmail(string? email, List<IdentityValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add(new("REQUIRED", "Email is required.", "Email"));
            return;
        }

        if (email.Length > MaxEmailLength)
        {
            errors.Add(new("MAX_LENGTH", $"Email must be at most {MaxEmailLength} characters.", "Email"));
            return;
        }

        if (!EmailPattern().IsMatch(email))
            errors.Add(new("INVALID_FORMAT", "Email format is invalid.", "Email"));
    }

    private static void ValidateUsername(string? username, List<IdentityValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            errors.Add(new("REQUIRED", "Username is required.", "Username"));
            return;
        }

        if (username.Length < MinUsernameLength)
        {
            errors.Add(new("MIN_LENGTH", $"Username must be at least {MinUsernameLength} characters.", "Username"));
            return;
        }

        if (username.Length > MaxUsernameLength)
        {
            errors.Add(new("MAX_LENGTH", $"Username must be at most {MaxUsernameLength} characters.", "Username"));
            return;
        }

        var normalized = username.ToLowerInvariant();
        if (!UsernameHandlePattern().IsMatch(normalized))
            errors.Add(new("INVALID_FORMAT", "Username can only contain letters, numbers, and underscores.", "Username"));
    }

    private static void ValidateHandle(string? handle, List<IdentityValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(handle))
        {
            errors.Add(new("REQUIRED", "Handle is required.", "Handle"));
            return;
        }

        if (handle.Length < MinHandleLength)
        {
            errors.Add(new("MIN_LENGTH", $"Handle must be at least {MinHandleLength} characters.", "Handle"));
            return;
        }

        if (handle.Length > MaxHandleLength)
        {
            errors.Add(new("MAX_LENGTH", $"Handle must be at most {MaxHandleLength} characters.", "Handle"));
            return;
        }

        var normalized = handle.ToLowerInvariant();
        if (!UsernameHandlePattern().IsMatch(normalized))
            errors.Add(new("INVALID_FORMAT", "Handle can only contain letters, numbers, and underscores.", "Handle"));
    }

    private static void ValidatePassword(string? password, List<IdentityValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add(new("REQUIRED", "Password is required.", "Password"));
            return;
        }

        if (password.Length < MinPasswordLength)
        {
            errors.Add(new("MIN_LENGTH", $"Password must be at least {MinPasswordLength} characters.", "Password"));
            return;
        }

        if (password.Length > MaxPasswordLength)
            errors.Add(new("MAX_LENGTH", $"Password must be at most {MaxPasswordLength} characters.", "Password"));
    }

    private static void ValidateDisplayName(string? displayName, string path, List<IdentityValidationError> errors)
    {
        if (displayName is not null && displayName.Length > MaxDisplayNameLength)
            errors.Add(new("MAX_LENGTH", $"{path} must be at most {MaxDisplayNameLength} characters.", path));
    }
}

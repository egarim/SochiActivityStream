namespace Identity.Core;

/// <summary>
/// Static methods for normalizing identity values.
/// </summary>
public static class IdentityNormalizer
{
    /// <summary>
    /// Normalizes an email address (trim + lowercase).
    /// </summary>
    public static string NormalizeEmail(string? email)
    {
        return email?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    /// <summary>
    /// Normalizes a username (trim + lowercase).
    /// </summary>
    public static string NormalizeUsername(string? username)
    {
        return username?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    /// <summary>
    /// Normalizes a profile handle (trim + lowercase).
    /// </summary>
    public static string NormalizeHandle(string? handle)
    {
        return handle?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    /// <summary>
    /// Normalizes a tenant ID (trim + lowercase).
    /// </summary>
    public static string NormalizeTenantId(string? tenantId)
    {
        return tenantId?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}

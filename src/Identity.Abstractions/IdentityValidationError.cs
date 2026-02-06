namespace Identity.Abstractions;

/// <summary>
/// Represents a validation error in the identity system.
/// </summary>
/// <param name="Code">Error code (e.g., "REQUIRED", "MAX_LENGTH", "DUPLICATE").</param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="Path">Optional path to the field (e.g., "Email", "Password").</param>
public sealed record IdentityValidationError(string Code, string Message, string? Path = null);

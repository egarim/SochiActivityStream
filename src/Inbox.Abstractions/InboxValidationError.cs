namespace Inbox.Abstractions;

/// <summary>
/// Represents a validation error for inbox operations.
/// </summary>
/// <param name="Code">Error code (e.g., "REQUIRED", "INVALID_FORMAT").</param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="Path">Optional path to the field that failed validation.</param>
public sealed record InboxValidationError(string Code, string Message, string? Path = null);

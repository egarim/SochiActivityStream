namespace ActivityStream.Abstractions;

/// <summary>
/// Represents a validation error for an activity.
/// </summary>
/// <param name="Code">Error code (e.g., "REQUIRED", "MAX_LENGTH").</param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="Path">Optional path to the field (e.g., "Actor.Kind").</param>
public sealed record ActivityValidationError(string Code, string Message, string? Path = null);

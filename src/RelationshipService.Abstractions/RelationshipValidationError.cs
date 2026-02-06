namespace RelationshipService.Abstractions;

/// <summary>
/// Represents a validation error for relationship operations.
/// </summary>
/// <param name="Code">Error code identifier.</param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="Path">Optional path to the field that caused the error.</param>
public sealed record RelationshipValidationError(string Code, string Message, string? Path = null);

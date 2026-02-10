namespace ActivityStream.Abstractions.Identity;

public sealed class IdentityValidationError
{
    public string? Field { get; set; }
    public string Message { get; set; } = string.Empty;
}

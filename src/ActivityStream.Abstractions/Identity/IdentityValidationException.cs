namespace ActivityStream.Abstractions.Identity;

public sealed class IdentityValidationException : Exception
{
    public IdentityValidationException(string message) : base(message) { }
}

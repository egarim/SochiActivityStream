namespace Chat.Abstractions;

/// <summary>
/// Exception thrown when chat validation fails.
/// </summary>
public sealed class ChatValidationException : Exception
{
    public ChatValidationError Error { get; }

    public ChatValidationException(ChatValidationError error)
        : base($"Chat validation failed: {error}")
    {
        Error = error;
    }

    public ChatValidationException(ChatValidationError error, string message)
        : base(message)
    {
        Error = error;
    }
}

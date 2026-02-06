namespace Chat.Core;

/// <summary>
/// Text normalization utilities for chat messages.
/// </summary>
public static class ChatNormalizer
{
    /// <summary>
    /// Normalizes a message body (trims whitespace).
    /// </summary>
    public static string NormalizeBody(string body)
    {
        if (string.IsNullOrEmpty(body))
            return body;

        return body.Trim();
    }

    /// <summary>
    /// Normalizes a conversation title.
    /// </summary>
    public static string? NormalizeTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return null;

        return title.Trim();
    }
}

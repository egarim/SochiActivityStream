namespace Content.Core;

/// <summary>
/// Text normalization utilities for content.
/// </summary>
public static class ContentNormalizer
{
    /// <summary>
    /// Normalizes text by trimming whitespace.
    /// </summary>
    public static string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text.Trim();
    }
}

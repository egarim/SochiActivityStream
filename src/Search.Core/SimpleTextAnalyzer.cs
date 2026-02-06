using Search.Abstractions;

namespace Search.Core;

/// <summary>
/// Simple text analyzer: lowercase, remove punctuation, split on whitespace.
/// </summary>
public sealed class SimpleTextAnalyzer : ITextAnalyzer
{
    private static readonly char[] Separators = 
    { 
        ' ', '\t', '\n', '\r', ',', '.', '!', '?', ';', ':', 
        '-', '_', '(', ')', '[', ']', '{', '}', '"', '\'',
        '/', '\\', '@', '#', '$', '%', '&', '*', '+', '=',
        '<', '>', '|', '~', '`'
    };

    private readonly int _minTokenLength;
    private readonly int _maxTokenLength;

    /// <summary>
    /// Creates a new SimpleTextAnalyzer.
    /// </summary>
    public SimpleTextAnalyzer(int minTokenLength = 2, int maxTokenLength = 100)
    {
        _minTokenLength = minTokenLength;
        _maxTokenLength = maxTokenLength;
    }

    /// <inheritdoc />
    public List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) 
            return new List<string>();

        return text
            .ToLowerInvariant()
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= _minTokenLength && t.Length <= _maxTokenLength)
            .Distinct()
            .ToList();
    }

    /// <inheritdoc />
    public string Normalize(string term)
    {
        return term.ToLowerInvariant().Trim();
    }
}

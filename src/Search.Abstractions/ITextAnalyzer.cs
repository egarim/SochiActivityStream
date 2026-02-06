namespace Search.Abstractions;

/// <summary>
/// Text analysis for indexing and searching.
/// </summary>
public interface ITextAnalyzer
{
    /// <summary>
    /// Tokenize text into searchable tokens.
    /// </summary>
    List<string> Tokenize(string text);

    /// <summary>
    /// Normalize a single term.
    /// </summary>
    string Normalize(string term);
}

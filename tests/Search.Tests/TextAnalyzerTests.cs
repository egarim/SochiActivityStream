using Search.Core;

namespace Search.Tests;

/// <summary>
/// Tests for SimpleTextAnalyzer.
/// </summary>
public class TextAnalyzerTests
{
    private readonly SimpleTextAnalyzer _analyzer;

    public TextAnalyzerTests()
    {
        _analyzer = new SimpleTextAnalyzer();
    }

    [Fact]
    public void Tokenize_simple_text_returns_lowercase_tokens()
    {
        var tokens = _analyzer.Tokenize("Hello World");

        Assert.Equal(2, tokens.Count);
        Assert.Contains("hello", tokens);
        Assert.Contains("world", tokens);
    }

    [Fact]
    public void Tokenize_removes_punctuation()
    {
        var tokens = _analyzer.Tokenize("Hello, World! How are you?");

        Assert.Contains("hello", tokens);
        Assert.Contains("world", tokens);
        Assert.Contains("how", tokens);
        Assert.Contains("are", tokens);
        Assert.Contains("you", tokens);
        Assert.DoesNotContain(",", tokens);
        Assert.DoesNotContain("!", tokens);
    }

    [Fact]
    public void Tokenize_removes_short_tokens()
    {
        var tokens = _analyzer.Tokenize("I am a developer");

        Assert.DoesNotContain("i", tokens);
        Assert.DoesNotContain("a", tokens);
        Assert.Contains("am", tokens);
        Assert.Contains("developer", tokens);
    }

    [Fact]
    public void Tokenize_returns_distinct_tokens()
    {
        var tokens = _analyzer.Tokenize("hello hello world world");

        Assert.Equal(2, tokens.Count);
        Assert.Contains("hello", tokens);
        Assert.Contains("world", tokens);
    }

    [Fact]
    public void Tokenize_empty_string_returns_empty_list()
    {
        var tokens = _analyzer.Tokenize("");

        Assert.Empty(tokens);
    }

    [Fact]
    public void Tokenize_null_string_returns_empty_list()
    {
        var tokens = _analyzer.Tokenize(null!);

        Assert.Empty(tokens);
    }

    [Fact]
    public void Tokenize_whitespace_only_returns_empty_list()
    {
        var tokens = _analyzer.Tokenize("   \t\n  ");

        Assert.Empty(tokens);
    }

    [Fact]
    public void Normalize_converts_to_lowercase()
    {
        var result = _analyzer.Normalize("HELLO");

        Assert.Equal("hello", result);
    }

    [Fact]
    public void Normalize_trims_whitespace()
    {
        var result = _analyzer.Normalize("  hello  ");

        Assert.Equal("hello", result);
    }

    [Fact]
    public void Tokenize_handles_special_characters()
    {
        var tokens = _analyzer.Tokenize("hello@world.com #test @user");

        Assert.Contains("hello", tokens);
        Assert.Contains("world", tokens);
        Assert.Contains("com", tokens);
        Assert.Contains("test", tokens);
        Assert.Contains("user", tokens);
    }

    [Fact]
    public void Custom_min_length_filters_short_tokens()
    {
        var analyzer = new SimpleTextAnalyzer(minTokenLength: 4);
        var tokens = analyzer.Tokenize("I am a developer from NYC");

        Assert.DoesNotContain("am", tokens);
        Assert.DoesNotContain("nyc", tokens);
        Assert.Contains("developer", tokens);
        Assert.Contains("from", tokens);
    }
}

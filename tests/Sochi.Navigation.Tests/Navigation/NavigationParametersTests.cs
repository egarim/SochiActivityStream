using Sochi.Navigation.Navigation;

namespace Sochi.Navigation.Tests.Navigation;

public sealed class NavigationParametersTests
{
    [Fact]
    public void Add_and_GetValue_returns_value()
    {
        // Arrange
        var parameters = new NavigationParameters();
        parameters.Add("key", "value");

        // Act
        var result = parameters.GetValue<string>("key");

        // Assert
        Assert.Equal("value", result);
    }

    [Fact]
    public void GetValue_with_missing_key_returns_default()
    {
        // Arrange
        var parameters = new NavigationParameters();

        // Act
        var result = parameters.GetValue<string>("missing");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetValue_converts_type()
    {
        // Arrange
        var parameters = new NavigationParameters();
        parameters.Add("id", "42");

        // Act
        var result = parameters.GetValue<int>("id");

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void TryGetValue_returns_true_when_key_exists()
    {
        // Arrange
        var parameters = new NavigationParameters();
        parameters.Add("key", "value");

        // Act
        var success = parameters.TryGetValue<string>("key", out var result);

        // Assert
        Assert.True(success);
        Assert.Equal("value", result);
    }

    [Fact]
    public void TryGetValue_returns_false_when_key_missing()
    {
        // Arrange
        var parameters = new NavigationParameters();

        // Act
        var success = parameters.TryGetValue<string>("missing", out var result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void ContainsKey_returns_true_for_existing_key()
    {
        // Arrange
        var parameters = new NavigationParameters();
        parameters.Add("key", "value");

        // Act & Assert
        Assert.True(parameters.ContainsKey("key"));
        Assert.False(parameters.ContainsKey("missing"));
    }

    [Fact]
    public void Count_returns_number_of_parameters()
    {
        // Arrange
        var parameters = new NavigationParameters();
        parameters.Add("key1", "value1");
        parameters.Add("key2", "value2");

        // Act & Assert
        Assert.Equal(2, parameters.Count);
    }

    [Fact]
    public void Constructor_parses_query_string()
    {
        // Arrange & Act
        var parameters = new NavigationParameters("?id=123&name=test");

        // Assert
        Assert.Equal("123", parameters.GetValue<string>("id"));
        Assert.Equal("test", parameters.GetValue<string>("name"));
    }

    [Fact]
    public void Constructor_parses_query_string_without_question_mark()
    {
        // Arrange & Act
        var parameters = new NavigationParameters("id=123");

        // Assert
        Assert.Equal("123", parameters.GetValue<string>("id"));
    }

    [Fact]
    public void ToQueryString_returns_formatted_string()
    {
        // Arrange
        var parameters = new NavigationParameters();
        parameters.Add("id", 123);
        parameters.Add("name", "test");

        // Act
        var result = parameters.ToQueryString();

        // Assert
        Assert.Contains("id=123", result);
        Assert.Contains("name=test", result);
        Assert.StartsWith("?", result);
    }

    [Fact]
    public void ToQueryString_returns_empty_for_no_parameters()
    {
        // Arrange
        var parameters = new NavigationParameters();

        // Act
        var result = parameters.ToQueryString();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Enumeration_iterates_all_parameters()
    {
        // Arrange
        var parameters = new NavigationParameters();
        parameters.Add("key1", "value1");
        parameters.Add("key2", "value2");

        // Act
        var count = 0;
        foreach (var _ in parameters)
        {
            count++;
        }

        // Assert
        Assert.Equal(2, count);
    }
}

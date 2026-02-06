using ActivityStream.Core;

namespace ActivityStream.Tests;

/// <summary>
/// Tests for cursor encoding/decoding.
/// </summary>
public class CursorTests
{
    [Fact]
    public void Cursor_roundtrip_preserves_data()
    {
        var occurredAt = new DateTimeOffset(2024, 6, 15, 14, 30, 45, 123, TimeSpan.Zero);
        var id = "01HX6Z1234567890ABCDEFGHJ";

        var cursor = CursorHelper.Encode(occurredAt, id);

        Assert.True(CursorHelper.TryDecode(cursor, out var decodedOccurredAt, out var decodedId));
        Assert.Equal(occurredAt, decodedOccurredAt);
        Assert.Equal(id, decodedId);
    }

    [Fact]
    public void Cursor_is_base64url_safe()
    {
        var occurredAt = DateTimeOffset.UtcNow;
        var id = "test-id";

        var cursor = CursorHelper.Encode(occurredAt, id);

        // Base64Url uses only alphanumeric, - and _
        Assert.Matches("^[A-Za-z0-9_-]+$", cursor);
    }

    [Fact]
    public void TryDecode_returns_false_for_null()
    {
        Assert.False(CursorHelper.TryDecode(null, out _, out _));
    }

    [Fact]
    public void TryDecode_returns_false_for_empty()
    {
        Assert.False(CursorHelper.TryDecode("", out _, out _));
    }

    [Fact]
    public void TryDecode_returns_false_for_invalid_base64()
    {
        Assert.False(CursorHelper.TryDecode("not-valid-base64!!!", out _, out _));
    }

    [Fact]
    public void TryDecode_returns_false_for_missing_separator()
    {
        // Encode something without separator
        var bytes = System.Text.Encoding.UTF8.GetBytes("noseparatorhere");
        var encoded = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        Assert.False(CursorHelper.TryDecode(encoded, out _, out _));
    }

    [Fact]
    public void TryDecode_returns_false_for_invalid_timestamp()
    {
        // Encode with invalid timestamp
        var bytes = System.Text.Encoding.UTF8.GetBytes("not-a-date|some-id");
        var encoded = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        Assert.False(CursorHelper.TryDecode(encoded, out _, out _));
    }

    [Fact]
    public void Cursor_handles_special_characters_in_id()
    {
        var occurredAt = DateTimeOffset.UtcNow;
        var id = "id/with+special=chars";

        var cursor = CursorHelper.Encode(occurredAt, id);

        Assert.True(CursorHelper.TryDecode(cursor, out _, out var decodedId));
        Assert.Equal(id, decodedId);
    }

    [Theory]
    [InlineData("2024-01-01T00:00:00+00:00")]
    [InlineData("2024-06-15T23:59:59.999+05:30")]
    [InlineData("2024-12-31T12:00:00-08:00")]
    public void Cursor_handles_various_timezone_offsets(string timestampStr)
    {
        var occurredAt = DateTimeOffset.Parse(timestampStr);
        var id = "test-id";

        var cursor = CursorHelper.Encode(occurredAt, id);

        Assert.True(CursorHelper.TryDecode(cursor, out var decoded, out _));
        Assert.Equal(occurredAt, decoded);
    }
}

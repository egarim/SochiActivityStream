using System.Buffers.Text;
using System.Text;

namespace ActivityStream.Core;

/// <summary>
/// Helper for encoding/decoding pagination cursors.
/// Cursor format: "{occurredAt:O}|{id}" encoded as Base64Url (no padding).
/// </summary>
public static class CursorHelper
{
    private const char Separator = '|';

    /// <summary>
    /// Encodes a cursor from the last item's OccurredAt and Id.
    /// </summary>
    public static string Encode(DateTimeOffset occurredAt, string id)
    {
        var raw = $"{occurredAt:O}{Separator}{id}";
        var bytes = Encoding.UTF8.GetBytes(raw);
        return Base64UrlEncode(bytes);
    }

    /// <summary>
    /// Decodes a cursor into OccurredAt and Id.
    /// </summary>
    /// <param name="cursor">The encoded cursor.</param>
    /// <param name="occurredAt">The decoded timestamp.</param>
    /// <param name="id">The decoded id.</param>
    /// <returns>True if successfully decoded, false otherwise.</returns>
    public static bool TryDecode(string? cursor, out DateTimeOffset occurredAt, out string id)
    {
        occurredAt = default;
        id = string.Empty;

        if (string.IsNullOrEmpty(cursor))
            return false;

        byte[]? bytes;
        try
        {
            bytes = Base64UrlDecode(cursor);
        }
        catch
        {
            return false;
        }

        var raw = Encoding.UTF8.GetString(bytes);
        var separatorIndex = raw.IndexOf(Separator);
        if (separatorIndex < 0)
            return false;

        var timestampPart = raw[..separatorIndex];
        var idPart = raw[(separatorIndex + 1)..];

        if (!DateTimeOffset.TryParse(timestampPart, out occurredAt))
            return false;

        id = idPart;
        return true;
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        var base64 = Convert.ToBase64String(bytes);
        // Replace + with -, / with _, remove padding =
        return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string input)
    {
        // Replace - with +, _ with /
        var base64 = input.Replace('-', '+').Replace('_', '/');
        // Add padding
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}

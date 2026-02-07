using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace BlazorBook.Web.Data.ValueConverters;

/// <summary>
/// Generic JSON value converter for serializing complex types to JSON strings in SQLite
/// </summary>
public class JsonValueConverter<T> : ValueConverter<T?, string?>
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public JsonValueConverter()
        : base(
            v => v == null ? null : JsonSerializer.Serialize(v, Options),
            v => v == null ? default : JsonSerializer.Deserialize<T>(v, Options))
    {
    }
}

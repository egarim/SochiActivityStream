namespace Media.Abstractions;

/// <summary>
/// DTO for passing uploaded file data between components
/// </summary>
public class UploadedFileDto
{
    public string Name { get; set; } = "";
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "";
}

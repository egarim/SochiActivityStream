namespace ActivityStream.Abstractions.Search;

public sealed class SearchDocument
{
    public string? Id { get; set; }
    public string? Source { get; set; }
    public string? Body { get; set; }
}

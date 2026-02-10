namespace ActivityStream.Abstractions.Realtime;

public sealed class RealtimeEvent
{
    public string? Type { get; set; }
    public object? Payload { get; set; }
}

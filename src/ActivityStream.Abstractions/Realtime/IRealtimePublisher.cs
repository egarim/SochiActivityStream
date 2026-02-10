namespace ActivityStream.Abstractions.Realtime;

public interface IRealtimePublisher
{
    Task PublishAsync(RealtimeEvent evt, CancellationToken ct = default);
}

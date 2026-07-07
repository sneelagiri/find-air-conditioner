namespace FindAirConditioners.Web.Core.Abstractions;

public interface IMessageQueue<TMessage>
{
    Task EnqueueAsync(TMessage message, CancellationToken cancellationToken = default);
    Task<TMessage?> DequeueAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
}

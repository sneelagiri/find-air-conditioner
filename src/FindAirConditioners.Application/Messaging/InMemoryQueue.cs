using System.Collections.Concurrent;
using FindAirConditioners.Application.Abstractions;

namespace FindAirConditioners.Application.Messaging;

public sealed class InMemoryQueue<TMessage> : IMessageQueue<TMessage>
{
    readonly ConcurrentQueue<TMessage> queue = new();
    readonly SemaphoreSlim signal = new(0);

    public Task EnqueueAsync(TMessage message, CancellationToken cancellationToken = default)
    {
        queue.Enqueue(message);
        signal.Release();
        return Task.CompletedTask;
    }

    public async Task<TMessage?> DequeueAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (!await signal.WaitAsync(timeout, cancellationToken))
        {
            return default;
        }

        return queue.TryDequeue(out var message) ? message : default;
    }
}

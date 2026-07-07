using System.Collections.Concurrent;
using FindAirConditioners.Application.Abstractions;

namespace FindAirConditioners.Infrastructure.Messaging;

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
        var waitTask = signal.WaitAsync(timeout, cancellationToken);
        if (!await waitTask)
        {
            return default;
        }

        return queue.TryDequeue(out var message) ? message : default;
    }
}

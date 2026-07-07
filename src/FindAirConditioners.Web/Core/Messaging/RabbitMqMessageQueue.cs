using System.Text;
using System.Text.Json;
using FindAirConditioners.Web.Core.Abstractions;
using RabbitMQ.Client;

namespace FindAirConditioners.Web.Core.Messaging;

public sealed class RabbitMqMessageQueue<TMessage> : IMessageQueue<TMessage>
{
    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    readonly ConnectionFactory connectionFactory;
    readonly string queueName;

    public RabbitMqMessageQueue(
        string hostName,
        int port,
        string userName,
        string password,
        string queueName)
    {
        connectionFactory = new ConnectionFactory
        {
            HostName = hostName,
            Port = port,
            UserName = userName,
            Password = password,
            DispatchConsumersAsync = false
        };

        this.queueName = queueName;
    }

    public Task EnqueueAsync(TMessage message, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

        var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, JsonOptions));
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: queueName,
            basicProperties: properties,
            body: payload);

        return Task.CompletedTask;
    }

    public async Task<TMessage?> DequeueAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var connection = connectionFactory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var response = channel.BasicGet(queueName, autoAck: false);
            if (response is not null)
            {
                channel.BasicAck(response.DeliveryTag, multiple: false);
                return JsonSerializer.Deserialize<TMessage>(response.Body.ToArray(), JsonOptions);
            }

            var delay = TimeSpan.FromMilliseconds(250);
            var remaining = deadline - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                break;
            }

            await Task.Delay(delay < remaining ? delay : remaining, cancellationToken);
        }

        return default;
    }
}

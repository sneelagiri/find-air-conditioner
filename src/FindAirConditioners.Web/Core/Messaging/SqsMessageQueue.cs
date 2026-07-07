using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using FindAirConditioners.Web.Core.Abstractions;

namespace FindAirConditioners.Web.Core.Messaging;

public sealed class SqsMessageQueue<TMessage>(IAmazonSQS sqsClient, string queueUrl) : IMessageQueue<TMessage>
{
    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task EnqueueAsync(TMessage message, CancellationToken cancellationToken = default)
    {
        await sqsClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = JsonSerializer.Serialize(message, JsonOptions)
        }, cancellationToken);
    }

    public async Task<TMessage?> DequeueAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var waitTime = Math.Clamp((int)Math.Ceiling(timeout.TotalSeconds), 1, 20);
        var response = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = queueUrl,
            MaxNumberOfMessages = 1,
            WaitTimeSeconds = waitTime
        }, cancellationToken);

        var message = response.Messages.FirstOrDefault();
        if (message is null)
        {
            return default;
        }

        await sqsClient.DeleteMessageAsync(new DeleteMessageRequest
        {
            QueueUrl = queueUrl,
            ReceiptHandle = message.ReceiptHandle
        }, cancellationToken);

        return JsonSerializer.Deserialize<TMessage>(message.Body, JsonOptions);
    }
}

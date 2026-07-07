namespace FindAirConditioners.Web.Core.Local;

public sealed record LocalAppConfig(
    string ConnectionString,
    string RabbitMqHost,
    int RabbitMqPort,
    string RabbitMqUsername,
    string RabbitMqPassword,
    string CollectQueueName,
    string AnalyzeQueueName,
    string NotifyQueueName);

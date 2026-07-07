namespace FindAirConditioners.Web.Core.Local;

public static class LocalAppConfigLoader
{
    public static LocalAppConfig Load(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("FindAirConditioners")
            ?? configuration["Local:ConnectionString"]
            ?? "Host=localhost;Port=5432;Database=findac;Username=findac;Password=findac_password;Pooling=true";

        var rabbitMqHost = configuration["RabbitMq:Host"]
            ?? Environment.GetEnvironmentVariable("RABBITMQ_HOST")
            ?? "localhost";

        var rabbitMqPort = int.TryParse(
            configuration["RabbitMq:Port"] ?? Environment.GetEnvironmentVariable("RABBITMQ_PORT"),
            out var parsedPort)
            ? parsedPort
            : 5672;

        var rabbitMqUsername = configuration["RabbitMq:Username"]
            ?? Environment.GetEnvironmentVariable("RABBITMQ_DEFAULT_USER")
            ?? "findac";

        var rabbitMqPassword = configuration["RabbitMq:Password"]
            ?? Environment.GetEnvironmentVariable("RABBITMQ_DEFAULT_PASS")
            ?? "findac_password";

        var collectQueueName = configuration["RabbitMq:CollectQueueName"] ?? "findac-collect";
        var analyzeQueueName = configuration["RabbitMq:AnalyzeQueueName"] ?? "findac-analyze";
        var notifyQueueName = configuration["RabbitMq:NotifyQueueName"] ?? "findac-notify";

        return new LocalAppConfig(
            connectionString,
            rabbitMqHost,
            rabbitMqPort,
            rabbitMqUsername,
            rabbitMqPassword,
            collectQueueName,
            analyzeQueueName,
            notifyQueueName);
    }
}

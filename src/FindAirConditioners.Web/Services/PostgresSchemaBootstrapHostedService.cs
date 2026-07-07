using FindAirConditioners.Web.Core.Persistence;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FindAirConditioners.Web.Services;

public sealed class PostgresSchemaBootstrapHostedService(
    PostgresAirConditionerRepository repository,
    ILogger<PostgresSchemaBootstrapHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Ensuring PostgreSQL schema exists.");
        await repository.InitializeAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

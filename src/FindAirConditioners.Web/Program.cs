using Amazon;
using Amazon.SecretsManager;
using Amazon.SQS;
using FindAirConditioners.Web.Components;
using FindAirConditioners.Web.Core.Aws;
using FindAirConditioners.Web.Core.Abstractions;
using FindAirConditioners.Web.Core.Messaging;
using FindAirConditioners.Web.Core.Local;
using FindAirConditioners.Web.Core.Models;
using FindAirConditioners.Web.Core.Options;
using FindAirConditioners.Web.Core.Scraping;
using FindAirConditioners.Web.Core.Persistence;
using FindAirConditioners.Web.Core.Services;
using FindAirConditioners.Web.Services;
using System.Text;

LoadDotEnvFile();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<SeededAirConditionerCatalog>();
builder.Services.Configure<ScraperOptions>(builder.Configuration.GetSection("Scraper"));
builder.Services.Configure<SchedulerOptions>(builder.Configuration.GetSection("Scheduler"));
builder.Services.Configure<NotificationOptions>(builder.Configuration.GetSection("Notifications"));
builder.Services.AddSingleton<IAirConditionerScraper, PlaywrightAirConditionerScraper>();
builder.Services.AddSingleton<IEmailNotificationSender, EmailNotificationSender>();

var appConfigSecretName = builder.Configuration["Aws:AppConfigSecretName"];
var awsRegionName = builder.Configuration["Aws:Region"] ?? Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-west-1";

if (!string.IsNullOrWhiteSpace(appConfigSecretName))
{
    var regionEndpoint = RegionEndpoint.GetBySystemName(awsRegionName);
    using var secretsClient = new AmazonSecretsManagerClient(regionEndpoint);
    var awsAppConfig = await AwsAppConfigLoader.LoadAsync(secretsClient, appConfigSecretName);

    var sqsClient = new AmazonSQSClient(regionEndpoint);
    var postgresRepository = new PostgresAirConditionerRepository(awsAppConfig.ConnectionString);
    var collectQueue = new SqsMessageQueue<CollectionJob>(sqsClient, awsAppConfig.CollectQueueUrl);
    var analyzeQueue = new SqsMessageQueue<AnalysisJob>(sqsClient, awsAppConfig.AnalyzeQueueUrl);
    var notifyQueue = new SqsMessageQueue<NotifyJob>(sqsClient, awsAppConfig.NotifyQueueUrl);

    builder.Services.AddSingleton<IAmazonSQS>(sqsClient);
    builder.Services.AddSingleton(postgresRepository);
    builder.Services.AddSingleton<IAirConditionerRepository>(postgresRepository);
    builder.Services.AddSingleton<IMessageQueue<CollectionJob>>(collectQueue);
    builder.Services.AddSingleton<IMessageQueue<AnalysisJob>>(analyzeQueue);
    builder.Services.AddSingleton<IMessageQueue<NotifyJob>>(notifyQueue);
    builder.Services.AddScoped<IAirConditionerCollectionService, AirConditionerCollectionService>();
    builder.Services.AddScoped<IAirConditionerAnalysisService, AirConditionerAnalysisService>();
    builder.Services.AddHostedService<PostgresSchemaBootstrapHostedService>();
    builder.Services.AddHostedService<HourlySearchSchedulerHostedService>();
    builder.Services.AddHostedService<CollectionQueueHostedService>();
    builder.Services.AddHostedService<AnalysisQueueHostedService>();
    builder.Services.AddHostedService<NotifyQueueHostedService>();
}
else
{
    var localAppConfig = LocalAppConfigLoader.Load(builder.Configuration);
    var postgresRepository = new PostgresAirConditionerRepository(localAppConfig.ConnectionString);
    var collectQueue = new RabbitMqMessageQueue<CollectionJob>(
        localAppConfig.RabbitMqHost,
        localAppConfig.RabbitMqPort,
        localAppConfig.RabbitMqUsername,
        localAppConfig.RabbitMqPassword,
        localAppConfig.CollectQueueName);
    var analyzeQueue = new RabbitMqMessageQueue<AnalysisJob>(
        localAppConfig.RabbitMqHost,
        localAppConfig.RabbitMqPort,
        localAppConfig.RabbitMqUsername,
        localAppConfig.RabbitMqPassword,
        localAppConfig.AnalyzeQueueName);
    var notifyQueue = new RabbitMqMessageQueue<NotifyJob>(
        localAppConfig.RabbitMqHost,
        localAppConfig.RabbitMqPort,
        localAppConfig.RabbitMqUsername,
        localAppConfig.RabbitMqPassword,
        localAppConfig.NotifyQueueName);

    builder.Services.AddSingleton(postgresRepository);
    builder.Services.AddSingleton<IAirConditionerRepository>(postgresRepository);
    builder.Services.AddSingleton<IMessageQueue<CollectionJob>>(collectQueue);
    builder.Services.AddSingleton<IMessageQueue<AnalysisJob>>(analyzeQueue);
    builder.Services.AddSingleton<IMessageQueue<NotifyJob>>(notifyQueue);
    builder.Services.AddScoped<IAirConditionerCollectionService, AirConditionerCollectionService>();
    builder.Services.AddScoped<IAirConditionerAnalysisService, AirConditionerAnalysisService>();
    builder.Services.AddHostedService<PostgresSchemaBootstrapHostedService>();
    builder.Services.AddHostedService<HourlySearchSchedulerHostedService>();
    builder.Services.AddHostedService<CollectionQueueHostedService>();
    builder.Services.AddHostedService<AnalysisQueueHostedService>();
    builder.Services.AddHostedService<NotifyQueueHostedService>();
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapPost("/api/searches", async (
    AirConditionerSearchRequest request,
    IAirConditionerCollectionService collectionService,
    CancellationToken cancellationToken) =>
{
    var searchId = await collectionService.QueueCollectionAsync(request, cancellationToken);
    return Results.Accepted($"/api/searches/{searchId}", new { searchId });
});

app.MapGet("/api/searches/{searchId:guid}", async (
    Guid searchId,
    IAirConditionerRepository repository,
    CancellationToken cancellationToken) =>
{
    var result = await repository.GetResultAsync(searchId, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static void LoadDotEnvFile()
{
    var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (currentDirectory is not null)
    {
        var envPath = Path.Combine(currentDirectory.FullName, ".env");
        if (File.Exists(envPath))
        {
            foreach (var line in File.ReadAllLines(envPath, Encoding.UTF8))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                {
                    continue;
                }

                var equalsIndex = trimmed.IndexOf('=');
                if (equalsIndex <= 0)
                {
                    continue;
                }

                var key = trimmed[..equalsIndex].Trim();
                var value = trimmed[(equalsIndex + 1)..].Trim().Trim('"');
                if (!string.IsNullOrWhiteSpace(key) && Environment.GetEnvironmentVariable(key) is null)
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }

            return;
        }

        currentDirectory = currentDirectory.Parent;
    }
}

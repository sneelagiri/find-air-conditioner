namespace FindAirConditioners.Web.Core.Aws;

public sealed record AwsAppConfig(
    string ConnectionString,
    string CollectQueueUrl,
    string AnalyzeQueueUrl,
    string NotifyQueueUrl);

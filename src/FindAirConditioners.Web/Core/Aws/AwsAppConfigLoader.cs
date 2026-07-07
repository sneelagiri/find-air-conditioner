using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace FindAirConditioners.Web.Core.Aws;

public static class AwsAppConfigLoader
{
    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<AwsAppConfig> LoadAsync(
        IAmazonSecretsManager secretsManager,
        string secretName,
        CancellationToken cancellationToken = default)
    {
        var response = await secretsManager.GetSecretValueAsync(new GetSecretValueRequest
        {
            SecretId = secretName
        }, cancellationToken);

        var secretJson = response.SecretString;
        if (string.IsNullOrWhiteSpace(secretJson))
        {
            throw new InvalidOperationException($"Secret '{secretName}' did not contain a JSON payload.");
        }

        return JsonSerializer.Deserialize<AwsAppConfig>(secretJson, JsonOptions)
            ?? throw new InvalidOperationException($"Secret '{secretName}' could not be parsed.");
    }
}

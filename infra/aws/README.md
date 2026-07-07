# AWS Infrastructure

This Terraform stack provisions:

- A publicly reachable PostgreSQL RDS instance
- Three SQS queues for collect, analyze, and notify jobs
- A Secrets Manager secret with the app connection string and queue URLs
- The default VPC and a permissive security group for simple access
- An ECR repository for the web image
- An App Runner service when enabled

## Usage

Terraform will use the normal AWS credential chain, so your `aws configure` profile or `AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY` environment variables are enough.

The App Runner service is disabled by default on the first `terraform apply` so you can push an initial container image to ECR first.

```bash
cd infra/aws
terraform init
terraform plan
terraform apply
```

The app reads the secret name from `Aws:AppConfigSecretName`. The secret payload should look like:

```json
{
  "ConnectionString": "...",
  "CollectQueueUrl": "...",
  "AnalyzeQueueUrl": "...",
  "NotifyQueueUrl": "..."
}
```

This stack is intentionally open for convenience. If you deploy it outside of a trusted environment, tighten the security group and make the database private before exposing it broadly.

## App Runner deployment

After the first `terraform apply`, push an image tag to the ECR repository shown by the `ecr_repository_url` output.

Then enable App Runner:

```bash
terraform apply -var="enable_apprunner_service=true"
```

Once enabled, the `apprunner_service_url` output gives you the public app URL.

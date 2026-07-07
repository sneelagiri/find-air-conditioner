# Find Air Conditioners

This repository is structured like so:

- Blazor web UI for requesting available air conditioners
- Collection and analysis workflows separated by queue-backed jobs
- Persistence and queue abstractions that use local Docker services by default or AWS-backed services when configured
- Unit and integration test projects for the core workflows

## Solution layout

- `src/FindAirConditioners.Web` - Blazor UI and minimal API endpoints
- `src/FindAirConditioners.Application` - workflow services and interfaces
- `src/FindAirConditioners.Infrastructure` - hosted queue workers and in-memory infrastructure helpers
- `src/FindAirConditioners.Domain` - shared models
- `tests/FindAirConditioners.UnitTests` - fast behavior tests
- `tests/FindAirConditioners.IntegrationTests` - integration tests for the pipeline behavior

## How the app runs

The web app currently has two runtime modes:

1. **Default local mode**
   - If `Aws:AppConfigSecretName` is not set, the app connects to local PostgreSQL and RabbitMQ containers.
   - This is the quickest way to run the full flow locally without touching AWS.
   - The app uses a PostgreSQL connection string that points at `localhost:5432` and RabbitMQ at `localhost:5672` by default.

2. **AWS mode**
   - If `Aws:AppConfigSecretName` is set, the app loads its PostgreSQL connection string and queue URLs from AWS Secrets Manager.
   - In this mode the app uses Amazon RDS PostgreSQL and Amazon SQS.

## Local Infrastructure

Use the local Compose file to start the app, PostgreSQL, and RabbitMQ together:

```bash
docker compose -f docker-compose.local.yml up --build
```

Copy `.env.example` to `.env` if you want to change credentials, ports, or sources and selectors.

This brings up:

- the web app on `http://localhost:8080`
- PostgreSQL on `localhost:5432`
- RabbitMQ on `localhost:5672`
- RabbitMQ management UI on `http://localhost:15672`
- MailHog SMTP on `localhost:1025`
- MailHog inbox UI on `http://localhost:8025`

The app expects these local services when `Aws:AppConfigSecretName` is **not** set.
If you want to override the local defaults, set these configuration values before starting the app:

- `ConnectionStrings__FindAirConditioners`
- `RabbitMq__Host`
- `RabbitMq__Port`
- `RabbitMq__Username`
- `RabbitMq__Password`
- `RabbitMq__CollectQueueName`
- `RabbitMq__AnalyzeQueueName`
- `RabbitMq__NotifyQueueName`

### Scraper configuration

The collection worker uses a real browser-backed scraper that can be configured with one or more sources under `Scraper:Sources`.

If no sources are configured, the app falls back to the seeded sample catalog so the pipeline still works out of the box.

Example configuration:

```json
{
  "Scraper": {
    "Sources": [
      {
        "Name": "ExampleStore",
        "UrlTemplate": "https://www.joybuy.nl/explore/koelende-airconditioner/HXiijHsL",
        "ListingSelector": ".SearchResult_productList__zcsx2 .sgm_pc.style_UK_product_card__BL9hp.style_vertical__sqgVF",
        "TitleSelector": "",
        "PriceSelector": "",
        "LinkSelector": "a",
        "ImageSelector": "img",
        "NotesSelector": "",
        "AvailabilitySelector": ".style_UK_OutOfStockText__NYUSc.productCartItem",
        "UnavailableKeywords": [
          "out of stock",
          "sold out",
          "unavailable",
          "niet beschikbaar",
          "niet beschibaar",
          "niet op voorraad",
          "uitverkocht",
          "not available"
        ]
      }
    ]
  }
}
```

The values can be set in `appsettings.Development.json`, environment variables, or in the deployment config.

For Joybuy specifically:

- `ListingSelector` should point at the card list items:
  - `.SearchResult_productList__zcsx2 .sgm_pc.style_UK_product_card__BL9hp.style_vertical__sqgVF`
- `AvailabilitySelector` should point at the out-of-stock badge:
  - `.style_UK_OutOfStockText__NYUSc.productCartItem`
- `UnavailableKeywords` should include the text variants you want to hide, including `Uitverkocht`
- `TitleSelector` and `PriceSelector` can stay blank if the page does not expose stable selectors; the scraper will infer them from the card text as a fallback

If a card contains any of the excluded availability phrases, it will be skipped and not returned to the UI.

Because the scraper uses Playwright, the machine running the app needs the Playwright browser binaries installed. In development, run the Playwright browser install step after `dotnet restore` or `dotnet build`.

```bash
pwsh bin/Debug/net8.0/playwright.ps1 install chromium
```

If `pwsh` is not installed, use the Node-based installer instead:

```bash
npx playwright install chromium
```

## Run The App

### Local development

1. Start the local containers:

```bash
docker compose -f docker-compose.local.yml up --build
```

2. Start the app in default local mode:

```bash
dotnet run --project src/FindAirConditioners.Web
```

3. Open the app in your browser at the local URL shown in the terminal.

If you want to see the queues in RabbitMQ, open the management UI at `http://localhost:15672`.

### How to see what the backend is doing

Watch the terminal where `dotnet run` is running. The app logs each stage of the pipeline:

- collection job queued
- collection job dequeued
- listings found for the postal code
- analysis job queued
- analysis job dequeued
- analysis result saved

### Hourly runs and notifications

The app can run scheduled searches every hour and email the results.

In the UI, there is a checkbox to opt into email notifications and a field to enter the email address. If enabled, the backend keeps that address with the request and sends the finished analysis by email.

Configure these values in `.env` for local runs:

- `Scheduler__Enabled=true`
- `Scheduler__IntervalMinutes=60`
- `Scheduler__Subscriptions__0__MaxPrice`
- `Scheduler__Subscriptions__0__NotificationEmail`
- `Notifications__SmtpHost=localhost`
- `Notifications__SmtpPort=1025`

For local testing, open MailHog at `http://localhost:8025` to see the messages.

For AWS runs, keep the scheduler values the same and point the notification settings at your SMTP relay, such as SES SMTP credentials.

### Docker

The repository includes a Docker image for the web app that already contains the Playwright browser binaries.

Use the local Compose file when you want the app, PostgreSQL, and RabbitMQ together:

```bash
docker compose -f docker-compose.local.yml up --build
```

The local Compose file reads `.env` automatically, so uncomment the Joybuy scraper values there if you want the container to scrape Joybuy instead of falling back to the seeded catalog.

Use the AWS Compose file when you want only the app container and you already provisioned RDS, SQS, and Secrets Manager:

```bash
docker compose -f docker-compose.aws.yml up --build
```

That starts only the web app container locally against AWS resources. It is useful for smoke testing, but it is not the production AWS deployment path.

If you want hourly email notifications in AWS, set the `Scheduler__*` values in `.env.aws` and configure `Notifications__SmtpHost` with your SMTP relay endpoint.

### Run tests

```bash
dotnet test FindAirConditioners.sln
```

## AWS Infrastructure

Terraform lives in infra/aws.

It provisions:

- Public PostgreSQL RDS
- SQS queues for collect/analyze/notify
- The default VPC and a permissive database security group
- A Secrets Manager secret containing the app configuration payload
- An ECR repository for the web app image
- An App Runner service, once enabled

Provision with:

```bash
cd infra/aws
terraform init
terraform apply
```

To run the app against AWS, set these configuration values:

- `Aws:Region`
- `Aws:AppConfigSecretName`

The secret should contain JSON with:

- `ConnectionString`
- `CollectQueueUrl`
- `AnalyzeQueueUrl`
- `NotifyQueueUrl`

### Deploy to AWS

1. Provision the backend resources:

```bash
cd infra/aws
terraform init
terraform apply
```

2. Build and push the web image to ECR.
   - The repo includes `.github/workflows/deploy-ecs.yml` for GitHub Actions.
   - Set these GitHub secrets:
     - `AWS_ACCESS_KEY_ID`
     - `AWS_SECRET_ACCESS_KEY`
     - `AWS_REGION`
     - `ECR_REPOSITORY_NAME`
   - You can get the repository name from Terraform with:

```bash
cd infra/aws
terraform output -raw ecr_repository_name
```

3. Enable the App Runner service:

```bash
cd infra/aws
terraform apply -var="enable_apprunner_service=true"
```

4. Use the `apprunner_service_url` Terraform output to access the app.



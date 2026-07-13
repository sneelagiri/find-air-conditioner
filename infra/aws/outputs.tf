output "db_endpoint" {
  value       = aws_db_instance.this.address
  description = "PostgreSQL endpoint address."
}

output "db_port" {
  value       = aws_db_instance.this.port
  description = "PostgreSQL port."
}

output "db_name" {
  value       = aws_db_instance.this.db_name
  description = "PostgreSQL database name."
}

output "db_username" {
  value       = aws_db_instance.this.username
  sensitive   = true
  description = "PostgreSQL master username."
}

output "db_password" {
  value       = random_password.db_password.result
  sensitive   = true
  description = "Generated PostgreSQL password."
}

output "collect_queue_url" {
  value       = aws_sqs_queue.collect.url
  description = "SQS queue URL for collection jobs."
}

output "analyze_queue_url" {
  value       = aws_sqs_queue.analyze.url
  description = "SQS queue URL for analysis jobs."
}

output "notify_queue_url" {
  value       = aws_sqs_queue.notify.url
  description = "SQS queue URL for notifications."
}

output "app_config_secret_name" {
  value       = aws_secretsmanager_secret.app_config.name
  description = "Secrets Manager secret name containing the app config."
}

output "ecr_repository_url" {
  value       = aws_ecr_repository.app.repository_url
  description = "ECR repository URL for the application image."
}

output "ecr_repository_name" {
  value       = aws_ecr_repository.app.name
  description = "ECR repository name for the application image."
}

output "ecs_cluster_name" {
  value       = aws_ecs_cluster.main.name
  description = "ECS cluster name."
}

output "ecs_service_name" {
  value       = aws_ecs_service.app.name
  description = "ECS service name."
}

output "ecs_task_definition_name" {
  value       = aws_ecs_task_definition.app.family
  description = "ECS task definition name."
}

output "alb_dns_name" {
  value       = aws_lb.main.dns_name
  description = "Load balancer DNS name - use this to access your app."
}

output "alb_arn" {
  value       = aws_lb.main.arn
  description = "Load balancer ARN."
}
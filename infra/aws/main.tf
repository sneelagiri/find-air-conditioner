locals {
  name_prefix = "${var.project_name}-${var.environment}"
  queue_names = {
    collect = "${local.name_prefix}-collect"
    analyze = "${local.name_prefix}-analyze"
    notify  = "${local.name_prefix}-notify"
  }
  ecr_repository_name    = "${local.name_prefix}-app"
  apprunner_service_name = "${local.name_prefix}-app"
}

data "aws_availability_zones" "available" {
  state = "available"
}

data "aws_vpc" "default" {
  default = true
}

data "aws_subnets" "default" {
  filter {
    name   = "vpc-id"
    values = [data.aws_vpc.default.id]
  }
}

resource "random_password" "db_password" {
  length  = 24
  special = true
}

resource "aws_ecr_repository" "app" {
  name                 = local.ecr_repository_name
  image_tag_mutability = "MUTABLE"

  image_scanning_configuration {
    scan_on_push = true
  }

  tags = {
    Name        = local.ecr_repository_name
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "aws_ecr_lifecycle_policy" "app" {
  repository = aws_ecr_repository.app.name

  policy = jsonencode({
    rules = [
      {
        rulePriority = 1
        description  = "Keep recent application images"
        selection = {
          tagStatus   = "any"
          countType   = "imageCountMoreThan"
          countNumber = 10
        }
        action = {
          type = "expire"
        }
      }
    ]
  })
}

resource "aws_security_group" "db" {
  name        = "${local.name_prefix}-db-sg"
  description = "Allow PostgreSQL from anywhere for simple access."
  vpc_id      = data.aws_vpc.default.id

  ingress {
    from_port   = 5432
    to_port     = 5432
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name        = "${local.name_prefix}-db-sg"
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "aws_db_subnet_group" "this" {
  name       = "${local.name_prefix}-db-subnets"
  subnet_ids = data.aws_subnets.default.ids

  tags = {
    Name        = "${local.name_prefix}-db-subnets"
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "aws_db_instance" "this" {
  identifier              = "${local.name_prefix}-db"
  engine                  = "postgres"
  engine_version          = "16"
  instance_class          = var.db_instance_class
  allocated_storage       = var.db_allocated_storage
  db_name                 = var.db_name
  username                = var.db_username
  password                = random_password.db_password.result
  port                    = 5432
  multi_az                = var.db_multi_az
  publicly_accessible     = true
  backup_retention_period = var.db_backup_retention_days
  storage_encrypted       = true
  skip_final_snapshot     = true
  deletion_protection     = false
  db_subnet_group_name    = aws_db_subnet_group.this.name
  vpc_security_group_ids  = [aws_security_group.db.id]

  tags = {
    Name        = "${local.name_prefix}-db"
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "aws_sqs_queue" "collect" {
  name                       = local.queue_names.collect
  visibility_timeout_seconds = 60
  message_retention_seconds  = 345600
  receive_wait_time_seconds  = 20

  tags = {
    Name        = local.queue_names.collect
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "aws_sqs_queue" "analyze" {
  name                       = local.queue_names.analyze
  visibility_timeout_seconds = 60
  message_retention_seconds  = 345600
  receive_wait_time_seconds  = 20

  tags = {
    Name        = local.queue_names.analyze
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "aws_sqs_queue" "notify" {
  name                       = local.queue_names.notify
  visibility_timeout_seconds = 60
  message_retention_seconds  = 345600
  receive_wait_time_seconds  = 20

  tags = {
    Name        = local.queue_names.notify
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "aws_secretsmanager_secret" "app_config" {
  name = "${local.name_prefix}/app-config"

  tags = {
    Name        = "${local.name_prefix}-app-config"
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "aws_secretsmanager_secret_version" "app_config" {
  secret_id = aws_secretsmanager_secret.app_config.id

  secret_string = jsonencode({
    ConnectionString = "Host=${aws_db_instance.this.address};Port=${aws_db_instance.this.port};Database=${var.db_name};Username=${var.db_username};Password=${random_password.db_password.result};SSL Mode=Require;Trust Server Certificate=true"
    CollectQueueUrl  = aws_sqs_queue.collect.url
    AnalyzeQueueUrl  = aws_sqs_queue.analyze.url
    NotifyQueueUrl   = aws_sqs_queue.notify.url
  })
}

data "aws_iam_policy_document" "apprunner_ecr_assume_role" {
  statement {
    effect = "Allow"

    principals {
      type        = "Service"
      identifiers = ["build.apprunner.amazonaws.com"]
    }

    actions = ["sts:AssumeRole"]
  }
}

data "aws_iam_policy_document" "apprunner_instance_assume_role" {
  statement {
    effect = "Allow"

    principals {
      type        = "Service"
      identifiers = ["tasks.apprunner.amazonaws.com"]
    }

    actions = ["sts:AssumeRole"]
  }
}

data "aws_iam_policy_document" "apprunner_ecr_access" {
  statement {
    effect = "Allow"
    actions = [
      "ecr:GetAuthorizationToken"
    ]
    resources = ["*"]
  }

  statement {
    effect = "Allow"
    actions = [
      "ecr:BatchCheckLayerAvailability",
      "ecr:BatchGetImage",
      "ecr:DescribeImages",
      "ecr:GetDownloadUrlForLayer"
    ]
    resources = [aws_ecr_repository.app.arn]
  }
}

data "aws_iam_policy_document" "apprunner_instance_access" {
  statement {
    effect = "Allow"
    actions = [
      "secretsmanager:GetSecretValue"
    ]
    resources = [aws_secretsmanager_secret.app_config.arn]
  }

  statement {
    effect = "Allow"
    actions = [
      "sqs:ChangeMessageVisibility",
      "sqs:DeleteMessage",
      "sqs:GetQueueAttributes",
      "sqs:GetQueueUrl",
      "sqs:ReceiveMessage",
      "sqs:SendMessage"
    ]
    resources = [
      aws_sqs_queue.collect.arn,
      aws_sqs_queue.analyze.arn,
      aws_sqs_queue.notify.arn
    ]
  }
}

resource "aws_iam_role" "apprunner_ecr_access" {
  name               = "${local.name_prefix}-apprunner-ecr-access"
  assume_role_policy = data.aws_iam_policy_document.apprunner_ecr_assume_role.json

  tags = {
    Name        = "${local.name_prefix}-apprunner-ecr-access"
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "aws_iam_role_policy" "apprunner_ecr_access" {
  name   = "${local.name_prefix}-apprunner-ecr-access"
  role   = aws_iam_role.apprunner_ecr_access.id
  policy = data.aws_iam_policy_document.apprunner_ecr_access.json
}

resource "aws_iam_role" "apprunner_instance" {
  name               = "${local.name_prefix}-apprunner-instance"
  assume_role_policy = data.aws_iam_policy_document.apprunner_instance_assume_role.json

  tags = {
    Name        = "${local.name_prefix}-apprunner-instance"
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "aws_iam_role_policy" "apprunner_instance" {
  name   = "${local.name_prefix}-apprunner-instance"
  role   = aws_iam_role.apprunner_instance.id
  policy = data.aws_iam_policy_document.apprunner_instance_access.json
}

resource "aws_apprunner_service" "web" {
  count        = var.enable_apprunner_service ? 1 : 0
  service_name = local.apprunner_service_name

  source_configuration {
    auto_deployments_enabled = true

    authentication_configuration {
      access_role_arn = aws_iam_role.apprunner_ecr_access.arn
    }

    image_repository {
      image_identifier      = "${aws_ecr_repository.app.repository_url}:${var.app_image_tag}"
      image_repository_type = "ECR"

      image_configuration {
        port = "8080"

        runtime_environment_variables = {
          ASPNETCORE_ENVIRONMENT   = "Production"
          ASPNETCORE_URLS          = "http://+:8080"
          AWS_REGION               = var.aws_region
          Aws__AppConfigSecretName = aws_secretsmanager_secret.app_config.name
        }
      }
    }
  }

  instance_configuration {
    instance_role_arn = aws_iam_role.apprunner_instance.arn
    cpu               = "1 vCPU"
    memory            = "2 GB"
  }

  health_check_configuration {
    protocol            = "HTTP"
    path                = "/"
    interval            = 10
    timeout             = 5
    healthy_threshold   = 1
    unhealthy_threshold = 5
  }

  tags = {
    Name        = local.apprunner_service_name
    Environment = var.environment
    Project     = var.project_name
  }
}

locals {
  name_prefix = "${var.project_name}-${var.environment}"
  queue_names = {
    collect = "${local.name_prefix}-collect"
    analyze = "${local.name_prefix}-analyze"
    notify  = "${local.name_prefix}-notify"
  }
  ecr_repository_name = "${local.name_prefix}-app"
  ecs_cluster_name    = "${local.name_prefix}-cluster"
  ecs_service_name    = "${local.name_prefix}-service"
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

resource "aws_security_group" "ecs" {
  name        = "${local.name_prefix}-ecs-sg"
  description = "Allow traffic to ECS tasks"
  vpc_id      = data.aws_vpc.default.id

  ingress {
    from_port   = 8080
    to_port     = 8080
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
    Name        = "${local.name_prefix}-ecs-sg"
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "aws_security_group" "alb" {
  name        = "${local.name_prefix}-alb-sg"
  description = "Allow HTTP/HTTPS to ALB"
  vpc_id      = data.aws_vpc.default.id

  ingress {
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  ingress {
    from_port   = 443
    to_port     = 443
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
    Name        = "${local.name_prefix}-alb-sg"
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
    ConnectionString = "Host=${aws_db_instance.this.address};Port=${aws_db_instance.this.port};Database=${var.db_name};Username=${var.db_username};Password=${random_password.db_password.result};SslMode=Require;"
    CollectQueueUrl  = aws_sqs_queue.collect.url
    AnalyzeQueueUrl  = aws_sqs_queue.analyze.url
    NotifyQueueUrl   = aws_sqs_queue.notify.url
  })
}

data "aws_iam_policy_document" "ecs_task_assume_role" {
  statement {
    effect = "Allow"

    principals {
      type        = "Service"
      identifiers = ["ecs-tasks.amazonaws.com"]
    }

    actions = ["sts:AssumeRole"]
  }
}

resource "aws_iam_role" "ecs_task_execution_role" {
  name               = "${local.name_prefix}-ecs-task-execution-role"
  assume_role_policy = data.aws_iam_policy_document.ecs_task_assume_role.json

  tags = {
    Name        = "${local.name_prefix}-ecs-task-execution-role"
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "aws_iam_role_policy_attachment" "ecs_task_execution_role_policy" {
  role       = aws_iam_role.ecs_task_execution_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

# IAM Role for task to access secrets and queues
data "aws_iam_policy_document" "ecs_task_role" {
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

resource "aws_iam_role" "ecs_task_role" {
  name               = "${local.name_prefix}-ecs-task-role"
  assume_role_policy = data.aws_iam_policy_document.ecs_task_assume_role.json

  tags = {
    Name        = "${local.name_prefix}-ecs-task-role"
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "aws_iam_role_policy" "ecs_task_role_policy" {
  name   = "${local.name_prefix}-ecs-task-role-policy"
  role   = aws_iam_role.ecs_task_role.id
  policy = data.aws_iam_policy_document.ecs_task_role.json
}

resource "aws_ecs_cluster" "main" {
  name = local.ecs_cluster_name

  tags = {
    Name        = local.ecs_cluster_name
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "aws_cloudwatch_log_group" "ecs" {
  name              = "/ecs/${local.name_prefix}"
  retention_in_days = 7

  tags = {
    Name        = "${local.name_prefix}-logs"
    Environment = var.environment
    Project     = var.project_name
  }
}

# ECS Task Definition
resource "aws_ecs_task_definition" "app" {
  family                   = local.name_prefix
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = "256"
  memory                   = "512"
  execution_role_arn       = aws_iam_role.ecs_task_execution_role.arn
  task_role_arn            = aws_iam_role.ecs_task_role.arn

  container_definitions = jsonencode([{
    name      = local.name_prefix
    image     = "${aws_ecr_repository.app.repository_url}:latest"
    essential = true

    portMappings = [{
      containerPort = 8080
      hostPort      = 8080
      protocol      = "tcp"
    }]

    environment = [
      {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      },
      {
        name  = "ASPNETCORE_URLS"
        value = "http://+:8080"
      },
      {
        name  = "AWS_REGION"
        value = var.aws_region
      },
      {
        name  = "Aws__AppConfigSecretName"
        value = aws_secretsmanager_secret.app_config.name
      },
      {
        name  = "Notifications__FromAddress"
        value = var.notifications_from_address
      },
      {
        name  = "Notifications__FromName"
        value = var.notifications_from_name
      },
      {
        name  = "Notifications__SmtpHost"
        value = var.notifications_smtp_host
      },
      {
        name  = "Notifications__SmtpPort"
        value = var.notifications_smtp_port
      },
      {
        name  = "Notifications__SmtpUsername"
        value = var.notifications_smtp_username
      },
      {
        name  = "Notifications__SmtpPassword"
        value = var.notifications_smtp_password
      },
      {
        name  = "Notifications__UseSsl"
        value = var.notifications_use_ssl
      },
      {
        name  = "Scheduler__Enabled"
        value = var.scheduler_enabled
      },
      {
        name  = "Scheduler__IntervalMinutes"
        value = var.scheduler_interval_minutes
      },
      {
        name  = "Scheduler__Subscriptions__0__MaxPrice"
        value = var.scheduler_max_price
      },
      {
        name  = "Scheduler__Subscriptions__0__NotificationEmail"
        value = var.scheduler_notification_email
      },
      {
        name  = "Scraper__Sources__0__Name"
        value = "Joybuy"
      },
      {
        name  = "Scraper__Sources__0__UrlTemplate"
        value = var.scraper_url_template
      },
      {
        name  = "Scraper__Sources__0__ListingSelector"
        value = var.scraper_listing_selector
      },
      {
        name  = "Scraper__Sources__0__LinkSelector"
        value = "a"
      },
      {
        name  = "Scraper__Sources__0__ImageSelector"
        value = "img"
      },
      {
        name  = "Scraper__Sources__0__AvailabilitySelector"
        value = var.scraper_availability_selector
      },
      {
        name  = "Scraper__Sources__0__UnavailableKeywords__0"
        value = "out of stock"
      },
      {
        name  = "Scraper__Sources__0__UnavailableKeywords__1"
        value = "sold out"
      },
      {
        name  = "Scraper__Sources__0__UnavailableKeywords__2"
        value = "unavailable"
      },
      {
        name  = "Scraper__Sources__0__UnavailableKeywords__3"
        value = "not available"
      },
      {
        name  = "Scraper__Sources__0__UnavailableKeywords__4"
        value = "niet beschikbaar"
      },
      {
        name  = "Scraper__Sources__0__UnavailableKeywords__5"
        value = "niet beschibaar"
      },
      {
        name  = "Scraper__Sources__0__UnavailableKeywords__6"
        value = "niet op voorraad"
      },
      {
        name  = "Scraper__Sources__0__UnavailableKeywords__7"
        value = "uitverkocht"
      }
    ]

    logConfiguration = {
      logDriver = "awslogs"
      options = {
        "awslogs-group"         = aws_cloudwatch_log_group.ecs.name
        "awslogs-region"        = var.aws_region
        "awslogs-stream-prefix" = "ecs"
      }
    }
  }])

  tags = {
    Name        = "${local.name_prefix}-task"
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "aws_lb" "main" {
  name               = "${local.name_prefix}-alb"
  internal           = false
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb.id]
  subnets            = data.aws_subnets.default.ids

  tags = {
    Name        = "${local.name_prefix}-alb"
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "aws_lb_target_group" "app" {
  name        = "${local.name_prefix}-tg"
  port        = 8080
  protocol    = "HTTP"
  vpc_id      = data.aws_vpc.default.id
  target_type = "ip"

  health_check {
    healthy_threshold   = 2
    unhealthy_threshold = 2
    timeout             = 3
    interval            = 30
    path                = "/"
    matcher             = "200-399"
  }

  tags = {
    Name        = "${local.name_prefix}-tg"
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "aws_lb_listener" "app" {
  load_balancer_arn = aws_lb.main.arn
  port              = 80
  protocol          = "HTTP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.app.arn
  }
}

resource "aws_ecs_service" "app" {
  name            = local.ecs_service_name
  cluster         = aws_ecs_cluster.main.id
  task_definition = aws_ecs_task_definition.app.arn
  desired_count   = 1
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = data.aws_subnets.default.ids
    security_groups  = [aws_security_group.ecs.id]
    assign_public_ip = true
  }

  load_balancer {
    target_group_arn = aws_lb_target_group.app.arn
    container_name   = local.name_prefix
    container_port   = 8080
  }

  depends_on = [
    aws_lb_listener.app,
    aws_iam_role_policy.ecs_task_role_policy
  ]

  tags = {
    Name        = local.ecs_service_name
    Environment = var.environment
    Project     = var.project_name
  }
}

resource "aws_autoscaling_target" "ecs_target" {
  max_capacity       = 3
  min_capacity       = 1
  resource_id        = "service/${aws_ecs_cluster.main.name}/${aws_ecs_service.app.name}"
  scalable_dimension = "ecs:service:DesiredCount"
  service_namespace  = "ecs"
}

resource "aws_autoscaling_policy" "ecs_policy_cpu" {
  name               = "${local.name_prefix}-cpu-autoscaling"
  policy_type        = "TargetTrackingScaling"
  resource_id        = aws_autoscaling_target.ecs_target.resource_id
  scalable_dimension = aws_autoscaling_target.ecs_target.scalable_dimension
  service_namespace  = aws_autoscaling_target.ecs_target.service_namespace

  target_tracking_scaling_policy_configuration {
    predefined_metric_specification {
      predefined_metric_type = "ECSServiceAverageCPUUtilization"
    }
    target_value = 70.0
  }
}

resource "aws_autoscaling_policy" "ecs_policy_memory" {
  name               = "${local.name_prefix}-memory-autoscaling"
  policy_type        = "TargetTrackingScaling"
  resource_id        = aws_autoscaling_target.ecs_target.resource_id
  scalable_dimension = aws_autoscaling_target.ecs_target.scalable_dimension
  service_namespace  = aws_autoscaling_target.ecs_target.service_namespace

  target_tracking_scaling_policy_configuration {
    predefined_metric_specification {
      predefined_metric_type = "ECSServiceAverageMemoryUtilization"
    }
    target_value = 80.0
  }
}

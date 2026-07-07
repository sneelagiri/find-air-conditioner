variable "aws_region" {
  description = "AWS region for all resources."
  type        = string
  default     = "eu-north-1"
}

variable "environment" {
  description = "Environment name used for resource naming."
  type        = string
  default     = "dev"
}

variable "project_name" {
  description = "Short project name used in resource names."
  type        = string
  default     = "find-air-conditioners"
}

variable "db_name" {
  description = "Initial PostgreSQL database name."
  type        = string
  default     = "findac"
}

variable "db_username" {
  description = "Master username for PostgreSQL."
  type        = string
  default     = "findac"
  sensitive   = true
}

variable "db_instance_class" {
  description = "RDS instance class."
  type        = string
  default     = "db.t4g.micro"
}

variable "db_allocated_storage" {
  description = "Allocated storage in GB."
  type        = number
  default     = 20
}

variable "db_backup_retention_days" {
  description = "How long automated backups are retained."
  type        = number
  default     = 1
}

variable "db_multi_az" {
  description = "Enable Multi-AZ deployment."
  type        = bool
  default     = false
}

variable "db_publicly_accessible" {
  description = "Whether the DB should have a public endpoint."
  type        = bool
  default     = true
}

variable "app_image_tag" {
  description = "Docker image tag App Runner should deploy."
  type        = string
  default     = "latest"
}

variable "enable_apprunner_service" {
  description = "Whether to create the App Runner service."
  type        = bool
  default     = false
}

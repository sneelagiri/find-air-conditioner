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

# Notification variables
variable "notifications_from_address" {
  description = "Email address to send notifications from."
  type        = string
  default     = "shashankn96@gmail.com"
  sensitive   = true
}

variable "notifications_from_name" {
  description = "Name for email notifications."
  type        = string
  default     = "Find Air Conditioners"
}

variable "notifications_smtp_host" {
  description = "SMTP host for sending emails."
  type        = string
  default     = "email-smtp.eu-north-1.amazonaws.com"
}

variable "notifications_smtp_port" {
  description = "SMTP port."
  type        = string
  default     = "587"
}

variable "notifications_smtp_username" {
  description = "SMTP username."
  type        = string
  sensitive   = true
}

variable "notifications_smtp_password" {
  description = "SMTP password."
  type        = string
  sensitive   = true
}

variable "notifications_use_ssl" {
  description = "Use SSL for SMTP."
  type        = string
  default     = "true"
}

# Scheduler variables
variable "scheduler_enabled" {
  description = "Enable scheduled jobs."
  type        = string
  default     = "true"
}

variable "scheduler_interval_minutes" {
  description = "Interval between scheduled jobs in minutes."
  type        = string
  default     = "60"
}

variable "scheduler_max_price" {
  description = "Maximum price filter for scheduler."
  type        = string
  default     = "700"
}

variable "scheduler_notification_email" {
  description = "Email to receive scheduler notifications."
  type        = string
  sensitive   = true
}

# Scraper variables
variable "scraper_url_template" {
  description = "URL template for scraper source."
  type        = string
  default     = "https://www.joybuy.nl/explore/koelende-airconditioner/HXiijHsL"
}

variable "scraper_listing_selector" {
  description = "CSS selector for listing items."
  type        = string
  default     = ".SearchResult_productList__zcsx2 .sgm_pc.style_UK_product_card__BL9hp.style_vertical__sqgVF"
}

variable "scraper_availability_selector" {
  description = "CSS selector for availability status."
  type        = string
  default     = ".style_UK_OutOfStockText__NYUSc.productCartItem"
}

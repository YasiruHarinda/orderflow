variable "subscription_id" {
  type = string
}

variable "project" {
  type    = string
  default = "orderflow"
}

variable "resource_group_name" {
  description = "Created by the bootstrap stack."
  type        = string
  default     = "rg-orderflow"
}

variable "location" {
  type    = string
  default = "southeastasia"
}

variable "node_vm_size" {
  type    = string
  default = "Standard_B2s_v2"
}

variable "node_min_count" {
  type    = number
  default = 2
}

variable "node_max_count" {
  type    = number
  default = 4
}

variable "enable_managed_postgres" {
  description = "Use Azure Database for PostgreSQL for prod (true) or the in-cluster one (false, cheaper)."
  type        = bool
  default     = true
}

variable "gitops_repo_url" {
  description = "The repository ArgoCD watches."
  type        = string
  default     = "https://github.com/YasiruHarinda/orderflow-gitops.git"
}

variable "monthly_budget" {
  description = "Monthly cost alert threshold, in the subscription's currency."
  type        = number
  default     = 30
}

variable "alert_email" {
  description = "Where budget alerts are sent. Empty = no budget alert."
  type        = string
  default     = ""
}

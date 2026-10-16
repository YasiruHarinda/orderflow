variable "subscription_id" {
  description = "Azure subscription to deploy into (az account show --query id -o tsv)."
  type        = string
}

variable "project" {
  description = "Short name used in every resource name."
  type        = string
  default     = "orderflow"

  validation {
    condition     = can(regex("^[a-z0-9]{3,12}$", var.project))
    error_message = "Use 3-12 lower-case letters or digits (it becomes part of a storage account name)."
  }
}

variable "location" {
  description = "Azure region."
  type        = string
  default     = "southeastasia"
}

variable "github_repository" {
  description = "The GitHub repository allowed to deploy, as owner/name."
  type        = string
  default     = "YasiruHarinda/orderflow"
}

variable "name" {
  description = "Globally unique, 3-24 characters."
  type        = string
}

variable "location" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "tenant_id" {
  type = string
}

variable "admin_principal_id" {
  description = "Who runs Terraform (gets write access to secrets)."
  type        = string
}

variable "reader_principal_ids" {
  description = "Identities that may read secrets, by a stable name."
  type        = map(string)
  default     = {}
}

variable "secrets" {
  description = "Secret name => value."
  type        = map(string)
  sensitive   = true
}

variable "tags" {
  type    = map(string)
  default = {}
}

variable "name" {
  type = string
}

variable "location" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "subnet_id" {
  type = string
}

variable "kubernetes_version" {
  description = "Kubernetes version, or null for the current AKS default."
  type        = string
  default     = null
}

variable "node_vm_size" {
  type    = string
  default = "Standard_B2s_v2" # 2 vCPU, 8 GiB, burstable: cheap for a demo
}

variable "node_min_count" {
  type    = number
  default = 2
}

variable "node_max_count" {
  type    = number
  default = 4
}

variable "tags" {
  type    = map(string)
  default = {}
}

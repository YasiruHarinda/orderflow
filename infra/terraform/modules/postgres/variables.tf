variable "name" {
  type = string
}

variable "location" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "vnet_id" {
  type = string
}

variable "subnet_id" {
  description = "A subnet delegated to Microsoft.DBforPostgreSQL/flexibleServers."
  type        = string
}

variable "admin_login" {
  type    = string
  default = "orderflow"
}

variable "admin_password" {
  type      = string
  sensitive = true
}

variable "sku_name" {
  type    = string
  default = "B_Standard_B1ms" # burstable, 1 vCore: the cheapest option
}

variable "databases" {
  type    = list(string)
  default = ["ordersdb", "inventorydb"]
}

variable "tags" {
  type    = map(string)
  default = {}
}

# Azure Database for PostgreSQL - Flexible Server, used by PRODUCTION.
# Managed means Azure handles backups (point-in-time restore), patching and failover.
# Private access only: it has no public IP; only the virtual network can reach it,
# and a private DNS zone gives it a name the AKS pods can resolve.

resource "azurerm_private_dns_zone" "this" {
  name                = "${var.name}.private.postgres.database.azure.com"
  resource_group_name = var.resource_group_name
  tags                = var.tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "this" {
  name                 = "link-${var.name}"
  private_dns_zone_id  = azurerm_private_dns_zone.this.id
  virtual_network_id   = var.vnet_id
  registration_enabled = false
  tags                 = var.tags
}

resource "azurerm_postgresql_flexible_server" "this" {
  name                          = "psql-${var.name}"
  location                      = var.location
  resource_group_name           = var.resource_group_name
  version                       = "17"
  delegated_subnet_id           = var.subnet_id
  private_dns_zone_id           = azurerm_private_dns_zone.this.id
  public_network_access_enabled = false

  administrator_login    = var.admin_login
  administrator_password = var.admin_password

  sku_name              = var.sku_name
  storage_mb            = 32768
  backup_retention_days = 7

  authentication {
    password_auth_enabled = true
  }

  tags = var.tags

  depends_on = [azurerm_private_dns_zone_virtual_network_link.this]

  lifecycle {
    ignore_changes = [zone] # Azure may pick a zone; don't fight it
  }
}

resource "azurerm_postgresql_flexible_server_database" "this" {
  for_each = toset(var.databases)

  name      = each.key
  server_id = azurerm_postgresql_flexible_server.this.id
  charset   = "UTF8"
  collation = "en_US.utf8"
}

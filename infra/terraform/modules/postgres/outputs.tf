output "fqdn" {
  value = azurerm_postgresql_flexible_server.this.fqdn
}

output "admin_login" {
  value = azurerm_postgresql_flexible_server.this.administrator_login
}

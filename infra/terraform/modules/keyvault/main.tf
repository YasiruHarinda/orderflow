# Azure Key Vault: the single place where secrets live. Nothing secret is stored in git.
# Access uses Azure RBAC roles:
#   Terraform (admin)        -> "Key Vault Secrets Officer" (write)
#   External Secrets (pods)  -> "Key Vault Secrets User"    (read only)

resource "azurerm_key_vault" "this" {
  name                       = var.name
  location                   = var.location
  resource_group_name        = var.resource_group_name
  tenant_id                  = var.tenant_id
  sku_name                   = "standard"
  rbac_authorization_enabled = true
  soft_delete_retention_days = 7
  purge_protection_enabled   = false # demo: lets "terraform destroy" remove it completely
  tags                       = var.tags
}

resource "azurerm_role_assignment" "admin" {
  scope                = azurerm_key_vault.this.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = var.admin_principal_id
}

resource "azurerm_role_assignment" "readers" {
  for_each = var.reader_principal_ids

  scope                = azurerm_key_vault.this.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = each.value
}

# New role assignments take a short while to reach Key Vault. Without this pause,
# writing the first secret can fail with "403 Forbidden" on a fresh deployment.
resource "time_sleep" "rbac_propagation" {
  create_duration = "60s"
  depends_on      = [azurerm_role_assignment.admin]
}

resource "azurerm_key_vault_secret" "this" {
  for_each = nonsensitive(toset(keys(var.secrets)))

  name         = each.key
  value        = var.secrets[each.key]
  key_vault_id = azurerm_key_vault.this.id
  content_type = "text/plain"

  depends_on = [time_sleep.rbac_propagation]
}

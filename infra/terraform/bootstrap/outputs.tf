# Copy these into GitHub: Settings -> Secrets and variables -> Actions -> Variables.
# None of them is a secret (OIDC means there is no password to protect).

output "AZURE_CLIENT_ID" {
  value = azurerm_user_assigned_identity.github.client_id
}

output "AZURE_TENANT_ID" {
  value = data.azurerm_client_config.current.tenant_id
}

output "AZURE_SUBSCRIPTION_ID" {
  value = var.subscription_id
}

output "TF_STATE_RESOURCE_GROUP" {
  value = azurerm_resource_group.state.name
}

output "TF_STATE_STORAGE_ACCOUNT" {
  value = azurerm_storage_account.state.name
}

output "APP_RESOURCE_GROUP" {
  value = azurerm_resource_group.app.name
}

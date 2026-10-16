output "name" {
  value = azurerm_kubernetes_cluster.this.name
}

output "oidc_issuer_url" {
  value = azurerm_kubernetes_cluster.this.oidc_issuer_url
}

output "kube_config" {
  description = "Connection details for the Helm provider."
  value       = azurerm_kubernetes_cluster.this.kube_config[0]
  sensitive   = true
}

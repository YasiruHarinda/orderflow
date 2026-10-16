output "aks_name" {
  value = module.aks.name
}

output "kubeconfig_command" {
  description = "Run this to use kubectl against the new cluster."
  value       = "az aks get-credentials --resource-group ${local.rg} --name ${module.aks.name}"
}

output "key_vault_name" {
  value = module.keyvault.name
}

output "argocd_password_command" {
  description = "ArgoCD's initial admin password (user: admin)."
  value       = "kubectl -n argocd get secret argocd-initial-admin-secret -o jsonpath='{.data.password}' | base64 -d"
}

output "postgres_fqdn" {
  value = var.enable_managed_postgres ? module.postgres[0].fqdn : "in-cluster"
}

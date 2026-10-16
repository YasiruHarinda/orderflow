provider "azurerm" {
  features {
    key_vault {
      purge_soft_delete_on_destroy = true # demo: a destroyed vault can be recreated with the same name
    }
  }
  subscription_id = var.subscription_id
}

# Helm talks to the new AKS cluster directly, to install ArgoCD once.
# After that, ArgoCD installs everything else from Git (GitOps).
provider "helm" {
  kubernetes = {
    host                   = module.aks.kube_config.host
    client_certificate     = base64decode(module.aks.kube_config.client_certificate)
    client_key             = base64decode(module.aks.kube_config.client_key)
    cluster_ca_certificate = base64decode(module.aks.kube_config.cluster_ca_certificate)
  }
}

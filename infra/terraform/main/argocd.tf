# Terraform's last job: install ArgoCD and point it at the gitops repository.
# From here on, Git is the source of truth. ArgoCD installs the platform (Envoy Gateway,
# cert-manager, External Secrets, Argo Rollouts, Prometheus, Grafana, Loki, Tempo) and
# OrderFlow itself, and keeps the cluster equal to what is in Git ("self-healing").

locals {
  argocd_chart_version      = "10.9.6"
  argocd_apps_chart_version = "2.0.6"
}

resource "helm_release" "argocd" {
  name             = "argocd"
  repository       = "https://argoproj.github.io/argo-helm"
  chart            = "argo-cd"
  version          = local.argocd_chart_version
  namespace        = "argocd"
  create_namespace = true
  wait             = true
  timeout          = 600

  values = [yamlencode({
    configs = {
      params = {
        "server.insecure" = true # TLS ends at the Envoy gateway, not inside the cluster
      }
    }
    dex = { enabled = false } # no SSO in this demo; log in with the generated admin password
    server = {
      resources = { requests = { cpu = "50m", memory = "128Mi" } }
    }
    controller = {
      resources = { requests = { cpu = "100m", memory = "512Mi" } }
    }
    repoServer = {
      resources = { requests = { cpu = "50m", memory = "256Mi" } }
    }
  })]
}

# The "app of apps": ONE Application that renders the bootstrap chart in the gitops
# repo, which creates every other Application. Cluster-specific values from Terraform
# (Key Vault URL, identity IDs) are handed over here, so nothing is hard-coded in Git.
resource "helm_release" "root_app" {
  name       = "argocd-root"
  repository = "https://argoproj.github.io/argo-helm"
  chart      = "argocd-apps"
  version    = local.argocd_apps_chart_version
  namespace  = "argocd"

  values = [yamlencode({
    applications = {
      root = {
        namespace = "argocd"
        project   = "default"
        source = {
          repoURL        = var.gitops_repo_url
          targetRevision = "main"
          path           = "bootstrap"
          helm = {
            valuesObject = {
              gitopsRepoUrl = var.gitops_repo_url
              azure = {
                tenantId                = data.azurerm_client_config.current.tenant_id
                keyVaultUrl             = module.keyvault.vault_uri
                externalSecretsClientId = azurerm_user_assigned_identity.external_secrets.client_id
              }
            }
          }
        }
        destination = {
          server    = "https://kubernetes.default.svc"
          namespace = "argocd"
        }
        syncPolicy = {
          automated = { prune = true, selfHeal = true }
        }
      }
    }
  })]

  depends_on = [helm_release.argocd]
}

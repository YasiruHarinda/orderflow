# MAIN STACK: everything OrderFlow needs in Azure.
#
#   network ──► AKS ──► ArgoCD (argocd.tf) ──► everything else, from Git
#           └─► PostgreSQL (prod)
#   Key Vault ◄── passwords generated here; External Secrets reads them into Kubernetes

data "azurerm_client_config" "current" {}

data "azurerm_resource_group" "app" {
  name = var.resource_group_name
}

locals {
  location = data.azurerm_resource_group.app.location
  rg       = data.azurerm_resource_group.app.name
  tags = {
    project    = var.project
    managed-by = "terraform"
    stack      = "main"
  }
}

resource "random_string" "suffix" {
  length  = 5
  special = false
  upper   = false
}

module "network" {
  source = "../modules/network"

  name                = var.project
  location            = local.location
  resource_group_name = local.rg
  tags                = local.tags
}

module "aks" {
  source = "../modules/aks"

  name                = var.project
  location            = local.location
  resource_group_name = local.rg
  subnet_id           = module.network.aks_subnet_id
  node_vm_size        = var.node_vm_size
  node_min_count      = var.node_min_count
  node_max_count      = var.node_max_count
  tags                = local.tags
}

# ---------- Passwords (generated once, stored only in Key Vault and Terraform state) ----------

resource "random_password" "this" {
  for_each = toset(["dev-postgres", "dev-rabbitmq", "prod-postgres", "prod-rabbitmq"])

  length  = 32
  special = false # avoids escaping problems in connection strings and URIs
}

module "postgres" {
  source = "../modules/postgres"
  count  = var.enable_managed_postgres ? 1 : 0

  name                = "${var.project}-${random_string.suffix.result}"
  location            = local.location
  resource_group_name = local.rg
  vnet_id             = module.network.vnet_id
  subnet_id           = module.network.postgres_subnet_id
  admin_password      = random_password.this["prod-postgres"].result
  tags                = local.tags
}

# ---------- Workload identity for External Secrets Operator ----------
# The ESO pods prove who they are with a Kubernetes service account token; Azure trusts
# tokens from THIS cluster's OIDC issuer for THIS service account only. No secret needed.

resource "azurerm_user_assigned_identity" "external_secrets" {
  name                = "id-${var.project}-external-secrets"
  location            = local.location
  resource_group_name = local.rg
  tags                = local.tags
}

resource "azurerm_federated_identity_credential" "external_secrets" {
  name                      = "aks-external-secrets"
  user_assigned_identity_id = azurerm_user_assigned_identity.external_secrets.id
  audience                  = ["api://AzureADTokenExchange"]
  issuer                    = module.aks.oidc_issuer_url
  subject                   = "system:serviceaccount:external-secrets:external-secrets"
}

# ---------- Key Vault ----------

module "keyvault" {
  source = "../modules/keyvault"

  name                = "kv-${var.project}-${random_string.suffix.result}"
  location            = local.location
  resource_group_name = local.rg
  tenant_id           = data.azurerm_client_config.current.tenant_id
  admin_principal_id  = data.azurerm_client_config.current.object_id
  reader_principal_ids = {
    external-secrets = azurerm_user_assigned_identity.external_secrets.principal_id
  }

  # External Secrets builds the connection strings from these (see the gitops repo).
  secrets = {
    dev-postgres-password  = random_password.this["dev-postgres"].result
    dev-rabbitmq-password  = random_password.this["dev-rabbitmq"].result
    prod-rabbitmq-password = random_password.this["prod-rabbitmq"].result
    prod-postgres-password = random_password.this["prod-postgres"].result
    prod-postgres-host     = var.enable_managed_postgres ? module.postgres[0].fqdn : "postgres"
    prod-postgres-user     = var.enable_managed_postgres ? module.postgres[0].admin_login : "orderflow"
  }

  tags = local.tags
}

# ---------- Cost control ----------

resource "azurerm_consumption_budget_resource_group" "this" {
  count = var.alert_email == "" ? 0 : 1

  name              = "budget-${var.project}"
  resource_group_id = data.azurerm_resource_group.app.id
  amount            = var.monthly_budget
  time_grain        = "Monthly"

  time_period {
    start_date = formatdate("YYYY-MM-01'T'00:00:00Z", timestamp())
  }

  notification {
    operator       = "GreaterThan"
    threshold      = 80
    threshold_type = "Actual"
    contact_emails = [var.alert_email]
  }

  notification {
    operator       = "GreaterThan"
    threshold      = 100
    threshold_type = "Forecasted"
    contact_emails = [var.alert_email]
  }

  lifecycle {
    ignore_changes = [time_period] # timestamp() changes every run; the start date only matters once
  }
}

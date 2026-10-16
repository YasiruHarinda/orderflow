# BOOTSTRAP: run ONCE, from your laptop, with your own Azure login (az login).
#
# It creates what the main stack needs before it can run in GitHub Actions:
#   1. a storage account for Terraform state (with locking and versioning)
#   2. the resource group the application lives in
#   3. an identity GitHub Actions can log in as using OIDC - no password or secret
#      is ever stored in GitHub; Azure trusts tokens issued for this repository only
#
# This stack keeps its own state locally (terraform.tfstate in this folder). That is
# normal for a bootstrap: it cannot store its state in storage it has not created yet.

terraform {
  required_version = ">= 1.9.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 5.10"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.9"
    }
  }
}

provider "azurerm" {
  features {}
  subscription_id     = var.subscription_id
  storage_use_azuread = true # storage account has shared keys disabled; use Entra ID
}

data "azurerm_client_config" "current" {}

resource "random_string" "suffix" {
  length  = 6
  special = false
  upper   = false
}

locals {
  tags = {
    project    = var.project
    managed-by = "terraform"
    stack      = "bootstrap"
  }
}

# ---------- Terraform state ----------

resource "azurerm_resource_group" "state" {
  name     = "rg-${var.project}-tfstate"
  location = var.location
  tags     = local.tags
}

resource "azurerm_storage_account" "state" {
  name                            = "st${var.project}tf${random_string.suffix.result}"
  resource_group_name             = azurerm_resource_group.state.name
  location                        = azurerm_resource_group.state.location
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  min_tls_version                 = "TLS1_2"
  allow_nested_items_to_be_public = false
  shared_access_key_enabled       = false # no storage keys: access only with Entra ID roles

  blob_properties {
    versioning_enabled = true # every state change is kept; a bad apply can be undone

    delete_retention_policy {
      days = 14
    }
  }

  tags = local.tags
}

resource "azurerm_storage_container" "state" {
  name                  = "tfstate"
  storage_account_id    = azurerm_storage_account.state.id
  container_access_type = "private"
}

# You (the person running bootstrap) can read and write state, to run the main stack locally.
resource "azurerm_role_assignment" "state_you" {
  scope                = azurerm_storage_account.state.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = data.azurerm_client_config.current.object_id
}

# ---------- Application resource group ----------

resource "azurerm_resource_group" "app" {
  name     = "rg-${var.project}"
  location = var.location
  tags     = merge(local.tags, { stack = "main" })
}

# ---------- GitHub Actions identity (OIDC, no secrets) ----------

resource "azurerm_user_assigned_identity" "github" {
  name                = "id-${var.project}-github"
  resource_group_name = azurerm_resource_group.state.name
  location            = azurerm_resource_group.state.location
  tags                = local.tags
}

# Azure only accepts GitHub tokens whose "subject" matches one of these exactly:
# the main branch, pull requests, and the protected "production" environment.
resource "azurerm_federated_identity_credential" "github" {
  for_each = {
    main         = "repo:${var.github_repository}:ref:refs/heads/main"
    pull-request = "repo:${var.github_repository}:pull_request"
    production   = "repo:${var.github_repository}:environment:production"
  }

  name                      = "github-${each.key}"
  user_assigned_identity_id = azurerm_user_assigned_identity.github.id
  audience                  = ["api://AzureADTokenExchange"]
  issuer                    = "https://token.actions.githubusercontent.com"
  subject                   = each.value
}

# Least privilege: GitHub can manage resources ONLY inside the application resource group.
resource "azurerm_role_assignment" "github_contributor" {
  scope                = azurerm_resource_group.app.id
  role_definition_name = "Contributor"
  principal_id         = azurerm_user_assigned_identity.github.principal_id
}

# Needed because the main stack grants roles (e.g. "read Key Vault secrets") to identities it creates.
resource "azurerm_role_assignment" "github_rbac_admin" {
  scope                = azurerm_resource_group.app.id
  role_definition_name = "Role Based Access Control Administrator"
  principal_id         = azurerm_user_assigned_identity.github.principal_id
}

resource "azurerm_role_assignment" "github_state" {
  scope                = azurerm_storage_container.state.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_user_assigned_identity.github.principal_id
}

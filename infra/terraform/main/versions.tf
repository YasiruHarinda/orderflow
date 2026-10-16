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
    time = {
      source  = "hashicorp/time"
      version = "~> 0.14"
    }
    helm = {
      source  = "hashicorp/helm"
      version = "~> 3.3"
    }
  }

  # Remote state in the storage account created by ../bootstrap, with Entra ID auth
  # and automatic locking (two people/pipelines can never apply at the same time).
  # The account name is passed at init time:
  #   terraform init -backend-config="resource_group_name=..." -backend-config="storage_account_name=..."
  backend "azurerm" {
    container_name   = "tfstate"
    key              = "orderflow.tfstate"
    use_azuread_auth = true
  }
}

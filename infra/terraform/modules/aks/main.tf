# Azure Kubernetes Service, sized for a demo but configured like production:
# - Azure CNI Overlay + Cilium: fast networking and NetworkPolicy enforcement (eBPF)
# - OIDC issuer + Workload Identity: pods get Azure access without any stored secret
# - automatic patch upgrades and node image updates
# - cluster autoscaler: nodes are added when pods don't fit, removed when idle

resource "azurerm_kubernetes_cluster" "this" {
  name                = "aks-${var.name}"
  location            = var.location
  resource_group_name = var.resource_group_name
  dns_prefix          = var.name
  kubernetes_version  = var.kubernetes_version # null = latest stable default
  sku_tier            = "Free"                 # "Standard" adds an SLA for real production

  automatic_upgrade_channel = "patch"
  node_os_upgrade_channel   = "NodeImage"

  oidc_issuer_enabled       = true
  workload_identity_enabled = true

  image_cleaner_enabled        = true # deletes unused images from nodes
  image_cleaner_interval_hours = 48

  default_node_pool {
    name                 = "system"
    vm_size              = var.node_vm_size
    vnet_subnet_id       = var.subnet_id
    os_sku               = "AzureLinux"
    auto_scaling_enabled = true
    min_count            = var.node_min_count
    max_count            = var.node_max_count
    max_pods             = 60

    upgrade_settings {
      max_surge = "33%"
    }
  }

  # "Manual" = we manage node pools ourselves (with the cluster autoscaler above),
  # instead of Node Auto Provisioning choosing VM sizes for us.
  node_provisioning_profile {
    mode = "Manual"
  }

  identity {
    type = "SystemAssigned"
  }

  network_profile {
    network_plugin      = "azure"
    network_plugin_mode = "overlay"
    network_data_plane  = "cilium"
    network_policy      = "cilium"
    pod_cidr            = "192.168.0.0/16"
    service_cidr        = "10.20.0.0/16"
    dns_service_ip      = "10.20.0.10"
    load_balancer_sku   = "standard"
    outbound_type       = "loadBalancer"
  }

  tags = var.tags

  lifecycle {
    # The autoscaler and automatic upgrades change these; Terraform should not undo that.
    ignore_changes = [
      default_node_pool[0].node_count,
      kubernetes_version,
    ]
  }
}

# The cluster needs to manage IPs and load balancers in our own subnet.
resource "azurerm_role_assignment" "network" {
  scope                = var.subnet_id
  role_definition_name = "Network Contributor"
  principal_id         = azurerm_kubernetes_cluster.this.identity[0].principal_id
}

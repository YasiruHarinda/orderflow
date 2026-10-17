<#
.SYNOPSIS
  Runs OrderFlow on a local Kubernetes cluster (k3d), using the same Helm chart as AKS.

.DESCRIPTION
  1. Creates a k3d cluster (Kubernetes inside Docker) with port 8080 mapped to its load balancer.
  2. Installs Envoy Gateway (Gateway API) as the front door.
  3. Deploys PostgreSQL, RabbitMQ and Redis with local secrets.
  4. Builds the four images locally and imports them into the cluster (no registry needed).
  5. Installs the OrderFlow Helm chart and waits until every pod is ready.

  Requires: Docker Desktop, k3d, kubectl, helm.
  Remove everything with:  k3d cluster delete orderflow

.EXAMPLE
  ./scripts/k3d-up.ps1
  curl http://localhost:8080/api/v1/products
#>
param(
    [string] $ClusterName = "orderflow",
    [string] $EnvoyGatewayVersion = "v1.9.1"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

function Step($message) { Write-Host "`n==> $message" -ForegroundColor Cyan }

Step "Creating k3d cluster '$ClusterName'"
$existing = k3d cluster list -o json | ConvertFrom-Json | Where-Object { $_.name -eq $ClusterName }
if (-not $existing) {
    # Traefik is disabled: Envoy Gateway is our front door, the same as in AKS.
    k3d cluster create $ClusterName --agents 1 -p "8080:80@loadbalancer" --k3s-arg "--disable=traefik@server:0" --wait
}

Step "Installing Envoy Gateway $EnvoyGatewayVersion (Gateway API)"
helm upgrade --install eg oci://docker.io/envoyproxy/gateway-helm --version $EnvoyGatewayVersion `
    --namespace envoy-gateway-system --create-namespace --wait
kubectl apply -f deploy/k8s/local/cluster/gatewayclass.yaml

Step "Deploying PostgreSQL, RabbitMQ, Redis and local secrets"
kubectl apply -k deploy/k8s/local
kubectl -n orderflow rollout status statefulset/postgres --timeout=180s
kubectl -n orderflow rollout status statefulset/rabbitmq --timeout=180s
kubectl -n orderflow rollout status deployment/redis --timeout=120s

Step "Building images and importing them into the cluster"
$images = @{
    "orders-api"           = "src/Orders/Orders.API/Dockerfile"
    "inventory-api"        = "src/Inventory/Inventory.API/Dockerfile"
    "notifications-worker" = "src/Notifications/Notifications.Worker/Dockerfile"
    "gateway"              = "src/Gateway/Dockerfile"
}
foreach ($service in $images.Keys) {
    docker build -t "orderflow/${service}:dev" -f $images[$service] .
    if ($LASTEXITCODE -ne 0) { throw "docker build failed for $service" }
}
k3d image import ($images.Keys | ForEach-Object { "orderflow/${_}:dev" }) -c $ClusterName

Step "Installing the OrderFlow Helm chart"
helm upgrade --install orderflow deploy/helm/orderflow `
    --namespace orderflow `
    -f deploy/helm/orderflow/values-local.yaml `
    --wait --timeout 10m

Step "Done"
kubectl -n orderflow get pods
Write-Host "`nTry it:" -ForegroundColor Green
Write-Host "  Open the store: http://localhost:8080"
Write-Host "  curl http://localhost:8080/api/v1/products"
Write-Host "  Remove the cluster: k3d cluster delete $ClusterName"

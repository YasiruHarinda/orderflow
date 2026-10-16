# Infrastructure (Terraform, Azure)

```
infra/terraform/
├── bootstrap/   run ONCE from your laptop: state storage + GitHub OIDC identity
├── main/        everything else, run by GitHub Actions (.github/workflows/infrastructure.yml)
└── modules/     network, aks, keyvault, postgres
```

## What gets created

| Resource | Why |
|---|---|
| Virtual network (2 subnets) | Private network for AKS nodes and PostgreSQL |
| AKS (Azure CNI Overlay + Cilium) | Runs everything; autoscaling 2–4 nodes |
| Azure Database for PostgreSQL | Production database (private access only) |
| Key Vault | All generated passwords; read by External Secrets |
| Managed identity + federated credential | Lets External Secrets read Key Vault without a secret |
| ArgoCD (Helm) | Takes over: installs everything else from the gitops repo |
| Budget alert | Email at 80% of the monthly budget |

## First-time setup (about 30 minutes)

1. **Log in to Azure**
   ```
   az login
   az account show --query id -o tsv     # your subscription id
   ```

2. **Run the bootstrap stack** (creates state storage and the GitHub identity)
   ```
   cd infra/terraform/bootstrap
   terraform init
   terraform apply -var="subscription_id=<your-subscription-id>"
   ```

3. **Tell GitHub about Azure.** In the repository: *Settings → Secrets and variables → Actions → Variables*, add the five outputs from step 2:
   `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `TF_STATE_RESOURCE_GROUP`, `TF_STATE_STORAGE_ACCOUNT`.
   They are not secrets: OIDC means there is no password.

4. **Protect production.** *Settings → Environments → New environment* `production` → add yourself as a *required reviewer*. Now every apply waits for your approval.

5. **Create the gitops repository** `orderflow-gitops` on GitHub and push the local `orderflow-gitops` folder to it (its README explains the layout). Then create a fine-grained token with *Contents* and *Pull requests* write access to it only. Save it as the repository **secret** `GITOPS_TOKEN`.

6. **Deploy.** *Actions → Infrastructure → Run workflow → apply*. About 15 minutes later:
   ```
   az aks get-credentials --resource-group rg-orderflow --name aks-orderflow
   kubectl get applications -n argocd     # ArgoCD installing everything
   ```

## Keep costs low

The cluster costs money every hour it runs. At the end of each day:
*Actions → Infrastructure → Run workflow → destroy*. Everything can be recreated from code, which is the point of Infrastructure as Code.

## Running Terraform locally

```
cd infra/terraform/main
cp terraform.tfvars.example terraform.tfvars    # set subscription_id
terraform init -backend-config="resource_group_name=<TF_STATE_RESOURCE_GROUP>" -backend-config="storage_account_name=<TF_STATE_STORAGE_ACCOUNT>"
terraform plan
```

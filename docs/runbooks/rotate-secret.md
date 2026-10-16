# Runbook: rotate a secret (e.g. the RabbitMQ password)

All secrets live in **Azure Key Vault**; Kubernetes Secrets are copies made by External Secrets (refreshed every hour). Nothing secret is in Git.

## Rotate a generated password (recommended)
Terraform generated it, so let Terraform generate a new one:
```
cd infra/terraform/main
terraform apply -replace='random_password.this["prod-rabbitmq"]'
```
This updates the Key Vault secret. Then:
1. **Force the copy now** instead of waiting up to an hour:
   ```
   kubectl -n orderflow-prod annotate externalsecret orderflow-orders-api force-sync=$(date +%s) --overwrite
   ```
   (repeat for `orderflow-inventory-api`, `orderflow-notifications-worker`, `orderflow-dependencies`)
2. **Restart the consumers of the secret**, since apps read connection strings at startup:
   ```
   kubectl -n orderflow-prod rollout restart statefulset/rabbitmq
   kubectl -n orderflow-prod rollout restart deployment/inventory-api deployment/notifications-worker
   kubectl argo rollouts restart orders-api -n orderflow-prod
   ```
3. **Verify**: pods ready, the smoke test passes (`scripts/smoke-test.sh http://<gateway-ip>`).

> RabbitMQ only applies `RABBITMQ_DEFAULT_PASS` when its data volume is created. For an existing broker, also run
> `kubectl -n orderflow-prod exec rabbitmq-0 -- rabbitmqctl change_password orderflow '<new password>'`.

## If a secret leaked
Rotate immediately as above, then check the gitleaks job and the repository's *Security → Secret scanning* for where it appeared, and write a postmortem.

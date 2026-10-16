# Runbook: failed or stuck deployment

## ArgoCD shows an application "Degraded" or "OutOfSync"
```
kubectl -n argocd get applications
kubectl -n argocd describe application orderflow-prod     # look at "Conditions" and "Sync Result"
```
| Cause | Sign | Action |
|---|---|---|
| Migration Job failed | `orders-api-migrate` Job in `Failed` | `kubectl -n orderflow-prod logs job/orders-api-migrate`. The old version is still running (the hook blocks the rollout). Fix the migration, release again. |
| Image cannot be pulled | Pods in `ImagePullBackOff` | Tag typo in gitops values, or the image is not public on GHCR |
| Pods never ready | `0/1 READY`, restarts | `kubectl describe pod`; check `/health/ready` dependencies and secrets (`ExternalSecret` status `SecretSynced`?) |
| Secret missing | `CreateContainerConfigError` | `kubectl -n orderflow-prod get externalsecrets`; Key Vault access (workload identity) |

## Canary (Argo Rollouts) aborted
That is the system working: the new version was unhealthy and traffic went back to the stable version.
```
kubectl argo rollouts get rollout orders-api -n orderflow-prod
kubectl -n orderflow-prod get analysisrun                    # which metric failed, with values
```
Then either fix and promote a new version, or make prod match what is running again by reverting the promotion commit in the gitops repo.

## Roll back on purpose
```
cd orderflow-gitops
git log --oneline envs/prod/values.yaml     # find the promotion commit
git revert <sha> && git push                # ArgoCD deploys the previous tag
```
Database migrations are not rolled back automatically. That's why migrations must stay backwards compatible (add columns; remove them only in a later release).

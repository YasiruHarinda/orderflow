# 0007. GitOps with ArgoCD and a separate gitops repository

- **Status:** Accepted
- **Date:** 2026-10-16

## Context
The usual alternative is "push" deployment: CI runs `kubectl apply` or `helm upgrade` against the cluster. That needs cluster admin credentials in CI, gives no record of what *should* be running, and does not notice manual changes (drift).

## Decision
- **ArgoCD** runs inside the cluster and **pulls** the desired state from Git (`orderflow-gitops`).
- **Terraform installs only ArgoCD** and one root Application ("app of apps"). Everything else, including the platform (gateway, cert-manager, External Secrets, Rollouts, observability), comes from Git.
- **Separate repository** for deployment state: CI's token can change image tags there without write access to application code, and deployment history is not mixed with code history.
- **Promotion**: CI commits new tags to `envs/dev` automatically and opens a **pull request** for `envs/prod`. Merging that PR *is* the production deployment. Rollback = `git revert`.
- **Self-healing**: manual changes in the cluster are reverted to what Git says.

## Consequences
- ✅ CI never holds cluster credentials. The cluster can be rebuilt from Git alone.
- ✅ Every production change has an author, a review and a timestamp.
- ❌ Two repositories to manage, and a short delay (ArgoCD polls every 3 minutes, or use a webhook).

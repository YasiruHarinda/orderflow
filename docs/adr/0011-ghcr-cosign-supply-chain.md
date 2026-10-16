# 0011. GHCR, Trivy, SBOM and cosign for the supply chain

- **Status:** Accepted
- **Date:** 2026-10-16
- Changes the first draft, which used Azure Container Registry.

## Context
Images must come from a trusted build, be free of known critical vulnerabilities, and be traceable to source. ACR costs money every day it exists, even when the cluster is destroyed.

## Decision
- **GitHub Container Registry** (free for public images, same identity as the code).
- Release pipeline per image: build → **Trivy** scan (fails on fixable HIGH/CRITICAL, results in the Security tab) → push → **SBOM** (SPDX) → **cosign keyless signature** (Sigstore, OIDC: no private key to protect).
- Images are tagged with the **commit SHA** (`sha-abc1234`) and GitOps deploys that tag. The release workflow never moves a SHA tag, and the cosign signature is bound to the image digest. (Pinning `tag@digest` in the gitops values would be the stricter next step.)
- **Chiseled** non-root base images; all GitHub Actions are **pinned to commit SHAs** (a tag can be moved by an attacker); Dependabot keeps them current; CodeQL and gitleaks run on every PR.

## Consequences
- ✅ Zero registry cost; every running image can be traced to a commit, a scan and a signature.
- ❌ The cluster does not *enforce* signatures yet. Next step: a Kyverno policy that only admits images signed by this repository's workflow.

# 0002. Environment-portable deployment

- **Status:** Accepted
- **Date:** 2026-11-15

## Context

ChattyDuck is meant to show how a distributed system runs in practice, not only how it is coded. It has to run on a developer machine and in at least two public clouds without forking the code.

## Decision

We will keep the system deployable to these targets:

- **Local:** .NET Aspire (inner loop), docker-compose, and a local Kubernetes distribution such as minikube.
- **Cloud:** Azure and GCP.

To make that possible:

- Every runnable service is containerised and exposes a health check endpoint.
- Every service reads its dependencies from configuration, with the same keys in every environment.
- Service logic uses no cloud-vendor SDKs. Vendor-specific services sit behind abstractions and each needs its own ADR.
- Every change keeps the existing deployment targets working. In the early stages a target may lag behind briefly, but only if the gap is recorded explicitly (a TODO in the deployment files or a note in the PR) and closed soon after.

## Alternatives considered

- **Single target (e.g. compose only):** simpler, but defeats the learning goal of running across environments. Rejected.
- **Cloud-native first (managed services everywhere):** faster to build, but ties the system to one vendor and makes local runs hard. Rejected.

## Consequences

- Each new service costs extra work, because every deployment target needs wiring for it.
- Configuration naming has to be consistent across services and environments.
- Follow-up: add an Aspire AppHost, Kubernetes manifests or a Helm chart, and cloud deployment definitions under `deploy/`.

# 0004. Local development environment: Aspire as the single source of every target

- **Status:** Proposed
- **Date:** 2026-11-16
- **Refines:** [0002](0002-environment-portable-deployment.md) (keeps every target, and decides how they are produced and in what order)
- **Related:** [0005](0005-envoy-as-edge-proxy-and-api-gateway.md) (Envoy as the edge proxy and API gateway)

## Context

ADR 0002 says ChattyDuck must run locally on .NET Aspire, docker-compose and a local Kubernetes distribution, and in the cloud on Azure and GCP. It does not say what each target is *for*, which one comes first, or how they share configuration. Maintaining several equal, hand-written descriptions of the same system would multiply the cost of every new service, which ADR 0002 already calls out as a consequence.

What exists today:

| Component | Layer | Language | Local state |
|---|---|---|---|
| Web UI | Client | TypeScript (React, Vite) | `npm run dev` on the host |
| Messenger | Edge | .NET | `dotnet run` only, no Dockerfile yet |
| Notifier | Edge | .NET | skeleton |
| SSO | Core | .NET | Dockerfile, in the hand-written `deploy/local/compose.yaml` |
| PostgreSQL | Storage | - | in `compose.yaml` |

What is planned: Envoy as the edge proxy and API gateway (ADR 0005), a Node.js BFF (ADR 0003), the Historical API, Hot Storage, Redis, RabbitMQ, MongoDB and blob storage. Within a few months the system will have around ten processes in at least two runtimes, plus four or five infrastructure containers.

The local environment has to serve several needs:

1. **Debug** any service with breakpoints, hot reload and fast restarts, while the rest of the system runs.
2. **Run the whole system as containers**, exactly as built by the Dockerfiles, behind Envoy on one public origin.
3. **Rehearse the cloud.** The long-term goal is to host on **Kubernetes in the cloud** (AKS and GKE), so the local Kubernetes descriptors should be the ones the cloud uses.
4. **Share configuration** across all of the above. Most settings (service names, routes, access rules, queue names, OIDC clients) are the same everywhere; only a few values differ.
5. **Stay technology-agnostic.** The Node BFF, Envoy and any future non-.NET service must be first-class citizens.
6. **Run on one developer machine** (Windows 11 Home, Docker Desktop on WSL2), with acceptable memory use.

Two things are easy to mix up and are treated separately below:

- **The orchestration model:** *which* processes run and how they are wired.
- **The configuration contract:** the keys each service reads, and plain files such as Envoy's configuration. This part must not depend on the tool that runs the system.

## Options evaluated

### A. docker compose, hand-written

One YAML file describes containers, networks, volumes and start order. Already in use.

- **Complexity:** low. Everybody can read it.
- **Maintenance:** each service needs its own block, and the same values get repeated without discipline.
- **Technology agnosticism:** complete.
- **Debugging:** weak inside containers. The usual workaround is hybrid: infrastructure in compose, the service under work in the IDE.
- **Production fidelity:** medium. Real images, but no probes, replicas, secrets objects or ingress.

### B. .NET Aspire AppHost, run mode only

A C# project describes the resources (containers, .NET projects, Node and Vite apps) and their references. `aspire run` starts everything, injects configuration, and opens a dashboard with logs, traces and metrics from every resource that speaks OpenTelemetry.

- **Complexity:** low to medium. One extra project, checked by the compiler.
- **Maintenance:** good. References and `WaitFor` replace hand-written URLs and start order.
- **Technology agnosticism:** medium. The AppHost is .NET, but it runs any container or Node app, and its dashboard accepts OTLP from any language. The risk is coupling *services* to Aspire, which the rules below prevent.
- **Debugging:** the best of all options. F5 attaches the debugger to every .NET service.
- **Production fidelity:** low on its own. .NET services run as host processes, not containers.

### C. Local Kubernetes (kind, minikube or k3d) with hand-written manifests

A single-node cluster on Docker, with Deployments, Services, ConfigMaps, Secrets and an ingress, organised with Kustomize or Helm.

- **Complexity:** high. Many concepts, and images have to be loaded into the cluster.
- **Maintenance:** medium. Verbose, but reusable for AKS and GKE.
- **Technology agnosticism:** complete.
- **Debugging:** weak without extra tooling.
- **Production fidelity:** the highest of the local options.
- **Resources:** around 6 to 8 GB of RAM for Docker once all services and infrastructure run.

### D. Kubernetes with an inner-loop tool (Tilt, Skaffold or DevSpace)

Same cluster as C, plus a tool that rebuilds images and syncs files into running pods. Better debugging than C and fully technology-agnostic, but another tool on top of Kubernetes, and .NET breakpoints inside pods are still clumsier than Aspire.

### E. Aspire as the single source, generating the other targets *(chosen)*

The AppHost is the only hand-written description of the system. In **run mode** it is the inner loop (option B). In **publish mode** it generates the other targets: a compose file through the Docker publisher (`AddDockerComposeEnvironment`) and a Helm chart through the Kubernetes publisher (`AddKubernetesEnvironment`), using `aspire publish`.

- **Pros:** one place to add a service; no drift between targets; the best debugging experience is also the default.
- **Cons:** the AppHost (C#) becomes the source of truth for every runtime. Some parts are not expressed by the model and need plain files next to it (Envoy configuration, certificates). The Docker and Kubernetes publishers are younger than the rest of Aspire, so some features may be missing or change.

### F. Dev Containers (and GitHub Codespaces)

A `devcontainer.json` that pins the toolchain. It complements any option. Optional follow-up.

### G. Hybrid "local process, remote cluster" (mirrord, Telepresence)

Run one service in the IDE against a cluster. Useful once the Kubernetes target is where most work happens. Later.

### Comparison

Scores are relative judgements for *this* project (5 is best, so "Setup" 5 means easiest).

| Criterion | A. compose | B. Aspire run only | C. local k8s | D. k8s + Tilt | E. Aspire generates all |
|---|---|---|---|---|---|
| Setup effort | 5 | 4 | 2 | 2 | 3 |
| Day-to-day maintenance | 3 | 4 | 2 | 3 | 4 |
| Debugging experience | 2 | 5 | 1 | 3 | 5 |
| Sharing common configuration | 3 | 4 | 4 | 4 | 5 |
| Technology agnosticism | 5 | 3 | 5 | 5 | 2 to 3 (see Decision 2) |
| Production / cloud fidelity | 3 | 2 | 5 | 5 | 4 (generated Helm) |
| Reuse towards cloud Kubernetes | 2 | 1 | 5 | 5 | 4 |
| Learning value | 3 | 4 | 5 | 4 | 3 (read the generated output) |
| Resource use on the laptop | 4 | 5 | 2 | 2 | 5 in run mode |

## Decision

### 1. Aspire is the default and the single source of every target

| Target | How it is produced | Purpose | When |
|---|---|---|---|
| **Inner loop** | `aspire run` on `deploy/aspire/ChattyDuck.AppHost` | Write and debug code; the default way to run ChattyDuck | Now |
| **Containers (compose)** | `aspire publish`, Docker publisher | Run the real images together behind Envoy | Now, replacing the hand-written `compose.yaml` once it reaches parity |
| **Local Kubernetes** | `aspire publish`, Kubernetes publisher (Helm chart), installed on kind or minikube | Rehearse the cloud deployment | No date set |
| **Cloud Kubernetes (AKS, GKE)** | The same Helm chart, with per-cloud values | Production-like hosting | Future ADR |

- **Generated output is committed** under `deploy/generated/<target>/` so that changes to the system show up as reviewable diffs, and so that someone without the .NET SDK can still read and run it. CI regenerates it and fails if the committed copy differs.
- **Generated files are never edited by hand.** If a publisher cannot express something, the gap is closed in this order: (1) a publisher customisation in the AppHost (Aspire lets you adjust the generated compose service or Kubernetes resource in C#), (2) a hand-written overlay applied on top of the output (a compose override file, or Helm values), (3) a TODO and a note in this ADR's follow-up list.
- **The hand-written `deploy/local/compose.yaml` stays** until the generated compose file runs the same services. Then it is deleted, and `deploy/local/README.md` moves to the generated target. Until then, the gap is visible here, as ADR 0002 requires.
- **A spike comes first.** Before services are added to the AppHost, a minimal AppHost with PostgreSQL, the SSO and Envoy is published to compose and Helm. The spike checks that bind-mounted Envoy files, secrets and the dev certificate come out usable. If the Kubernetes publisher cannot carry them, the fallback is a small hand-written Helm chart for Envoy alongside the generated one.

### 2. Aspire is a tool, not a dependency: one configuration contract owned by the services

Making a C# project the source of every target weakens "not .NET only" unless the services stay independent of it. So:

- Each service documents the configuration keys it reads (for example `ConnectionStrings__Identity`, `Sso__Authority`, `RabbitMq__Host`). The list lives in `docs/configuration.md`. Every target supplies the **same keys** with different values, as ADR 0002 requires.
- Services read **only our own keys**. They do not read Aspire's service-discovery variables (`services__sso__https__0`) and do not reference Aspire packages for discovery. In the AppHost our keys are set explicitly, for example `.WithEnvironment("Sso__Authority", ...)`. Connection strings follow the ASP.NET Core `ConnectionStrings__<name>` convention, which Aspire also uses, so naming the Aspire resource after the key is enough.
- Environment-specific plain files (Envoy) stay plain files that any tool can mount, not C# code.
- The generated compose file and Helm chart are standard formats. Anyone can read, run or replace them without Aspire.
- If Aspire is ever dropped, the generated output is the starting point for hand-written descriptors. Nothing inside a service changes.

### 3. Envoy runs in every target, so there is one public origin everywhere

ADR 0005 makes Envoy the edge proxy *and* the API gateway. It therefore runs in the inner loop too, not only in containers. The browser and the mobile client always talk to Envoy, and the SSO issuer, redirect URIs and cookies are the same in every target.

In the inner loop, Envoy is a container (`AddContainer` with a pinned `envoyproxy/envoy` tag), while the services run as host processes. Envoy reaches them through `host.docker.internal`. Envoy does not substitute environment variables in its configuration, so the configuration is **split by what changes**, using Envoy's file-based dynamic configuration:

| File | Contents | Varies by target? |
|---|---|---|
| `deploy/shared/envoy/envoy.yaml` | Bootstrap: admin interface, pointers to the two files below | No |
| `deploy/shared/envoy/lds.yaml` | Listeners, routes, JWT and RBAC rules (ADR 0005) | **No.** This is the contract |
| `deploy/shared/envoy/cds.aspire.yaml` | Clusters: `host.docker.internal:<fixed port>` | Aspire run mode only |
| `deploy/shared/envoy/cds.containers.yaml` | Clusters: `sso:8080`, `messenger:8080`, … | Compose and Kubernetes |

The AppHost mounts `cds.aspire.yaml` in run mode and `cds.containers.yaml` in publish mode (`builder.ExecutionContext.IsRunMode`). Routes and access rules are written once.

For this to work, two conventions apply:

- **Fixed ports in run mode.** Each service gets a fixed HTTP port in the AppHost, so `cds.aspire.yaml` can name it:

  | Resource | Run mode (host) | Containers |
  |---|---|---|
  | `envoy` public listener | `https://localhost:8443` | `8443` |
  | `envoy` internal listener | `localhost:10000` | `10000`, not published |
  | `sso` | `5101` | `8080` |
  | `messenger` | `5102` | `8080` |
  | `bff` | `5103` | `8080` |
  | `web` (Vite dev server) | `5173` | served by the BFF |
  | `historical-api`, `hot-storage`, … | `5104` and up | `8080` |

- **Same names everywhere.** The Aspire resource name equals the compose service name equals the Kubernetes Service name. Every service listens on `8080` inside its container. That is why one `cds.containers.yaml` serves both compose and Kubernetes.

### 4. Observability and health are standards, not Aspire features

- Every service, in any language, exports OpenTelemetry over OTLP to the address in `OTEL_EXPORTER_OTLP_ENDPOINT`, and exposes `/health/live` and `/health/ready`.
- The Aspire dashboard receives that telemetry in run mode. In generated compose, the standalone Aspire dashboard container plays the same role. In Kubernetes and the cloud, the same variable points at an OpenTelemetry collector.
- .NET services may use an Aspire **ServiceDefaults** project for OpenTelemetry, health checks and resilience. It is a convenience library for .NET only, not a contract between services, and it only configures things a non-.NET service can implement on its own.

### 5. Secrets

- Secrets are Aspire **parameters** marked as secret. In run mode their values come from `dotnet user-secrets` on the AppHost. In generated compose they become variables in a git-ignored `.env`; in the Helm chart they become Kubernetes Secrets whose values are supplied at install time.
- Generated output never contains secret values. That is the seam that cloud secret stores (Azure Key Vault, GCP Secret Manager, through the Secrets Store CSI driver or External Secrets) plug into later.

### 6. Infrastructure as code: what is baked in now, what waits for the cloud

"Infrastructure as code" covers two layers with different timing:

| Layer | Examples | Now | Cloud |
|---|---|---|---|
| **Application deployment** | The AppHost model, generated compose and Helm chart, Envoy files | **Now.** Declarative, in git, and generated from one model. | The same Helm chart with per-cloud values. |
| **Infrastructure provisioning** | Clusters, managed PostgreSQL, container registries, networking, DNS, key vaults | Nothing to provision locally except a kind cluster, created from a checked-in `deploy/k8s/kind.yaml` when that target starts. | Terraform or OpenTofu, in its own ADR. |

The local design keeps the cloud step open:

- **The configuration contract is the interface between provisioning and deployment.** Terraform outputs (a database host, a Key Vault URI) are written into Helm values for the same keys a service already reads.
- **Infrastructure is swappable per environment.** Locally, PostgreSQL, RabbitMQ and Redis are containers in the model. In the cloud, the Helm values switch them off and point the same keys at managed services.
- **Images are built once and promoted.** Only configuration differs between environments.
- **Multi-cloud bias.** Aspire's own deploy path (`aspire deploy`, azd, Bicep) targets Azure only. To keep AKS and GKE equal, cloud provisioning uses Terraform or OpenTofu (or Pulumi in C#), not Aspire's Azure deploy. That choice belongs to the cloud ADR.

## Alternatives considered

- **Hand-written compose and Kubernetes descriptors next to Aspire run mode.** The best for learning those formats, and fully independent of Aspire. Rejected because keeping three descriptions in sync by hand is the cost this ADR tries to avoid. The generated output is still committed and readable, so the formats are still visible.
- **Compose as the default.** The most neutral choice, but debugging several .NET services at once is much weaker. Rejected.
- **Kubernetes with Tilt as the only local environment (option D).** The highest fidelity, but the heaviest on setup and laptop resources while most services do not exist yet. Can be revisited in a new ADR.
- **Generating Kubernetes manifests from compose (Kompose).** One-off conversion output that needs heavy editing. Rejected.
- **YARP instead of Envoy, because Aspire supports it natively.** It would remove the fixed-port convention and publish cleanly, but it is .NET and does not match Envoy-based ingress on Kubernetes. ADR 0005 keeps Envoy.
- **Templating Envoy's configuration at container start (envsubst).** Avoids fixed ports, but needs a custom Envoy image and a script. The split routes/clusters files do the same with standard Envoy features. Rejected.

## Consequences

- **One place to add a service:** a Dockerfile, an AppHost entry with its keys and fixed port, an entry in `cds.*.yaml`, and its routes in `lds.yaml`. Everything else is generated.
- **The AppHost is central.** Running ChattyDuck the default way needs the .NET SDK, even for someone working only on the Node BFF. The committed generated compose file is the fallback for them.
- **Aspire version upgrades can change the generated output.** That shows up as a diff in CI, which is the point of committing it.
- **Publishers may not cover everything,** especially bind-mounted configuration in the Helm chart. The spike in Decision 1 finds this out early, and the fallback order is defined.
- **The SSO configuration changes:**
  - its connection string key is renamed from `ConnectionStrings__DefaultConnection` to a logical name (for example `ConnectionStrings__Identity`);
  - its public issuer becomes the Envoy origin in every target, and `chatty-web-client`'s redirect URIs move from `https://localhost:5002` to that origin;
  - forwarded-headers handling (already present in `ForwardedHeadersSettings`) is needed in the inner loop too, trusting only Envoy.
- **Certificates:** the .NET dev certificate is mounted into the Envoy container in every target, as the SSO does today. Services behind Envoy use plain HTTP.
- **Fixed ports can clash** with other software on the machine. They are defined once in the AppHost, so changing one is a one-line change plus `cds.aspire.yaml`.

### Follow-up work

- [ ] Spike: a minimal AppHost with PostgreSQL, the SSO and Envoy, published to compose and Helm (Decision 1). Record what does not come out cleanly.
- [ ] Create `deploy/aspire/ChattyDuck.AppHost` (and optionally `ChattyDuck.ServiceDefaults`), and add them to `ChattyDuck.slnx` under a `/Deploy/` solution folder. Use the `duende-skills:identityserver-aspire` skill for the SSO wiring.
- [ ] Write `docs/configuration.md` with the configuration contract, and rename the SSO connection string key.
- [ ] Add `deploy/shared/envoy/` with the bootstrap, `lds.yaml` and the two cluster files.
- [ ] Add health endpoints to the SSO and Messenger, and a Dockerfile for Messenger (ADR 0002 gaps).
- [ ] Generate `deploy/generated/compose`, reach parity with `deploy/local/compose.yaml`, then delete the hand-written file.
- [ ] Add a CI job that regenerates the output and fails on differences, and validates the Envoy configuration (`envoy --mode validate`).
- [ ] When the local Kubernetes target starts: add `deploy/k8s/kind.yaml` and install the generated Helm chart.
- [ ] Write the cloud deployment ADR (AKS and GKE; Terraform/OpenTofu vs Pulumi for provisioning).

# ChattyDuck

ChattyDuck is a **learning project**: a real-time chat system built mainly with C#/.NET, designed so other technologies can plug in alongside it (for example a service in another language, a different client stack, or a different storage engine).
The goal is to practise production-style distributed-system design (identity, real-time messaging, message brokering, caching, persistence, deployment) at a scale that one person can keep in their head.

Because this is a learning project, clarity and explicit reasoning matter more than speed. Prefer approaches that are idiomatic and easy to explain over clever shortcuts.

## Design principles

- **.NET first, not .NET only.** Services talk over standard, technology-neutral contracts (HTTP/REST, WebSocket/SignalR, AMQP via RabbitMQ, OIDC/OAuth2 via the SSO). Never couple services through shared .NET assemblies or in-process types that a non-.NET component could not use.
- **Layered architecture.** Every component belongs to exactly one layer: Client, Edge, Core or Storage. Dependencies flow inward: Client → Edge → Core → Storage. Edge services must not reach Storage directly.
- **Configuration over code for the environment.** Connection strings, endpoints, secrets and feature switches come from configuration (appsettings, environment variables, secret stores). Code must not hard-code environment specifics.
- **Decisions are written down.** Every architectural decision or change is recorded as an ADR (see below).

## Architecture: where it lives

This file does **not** hold the architecture itself. The source of truth is `/docs`:

| What | Where |
|---|---|
| System overview diagram (layers, services, infrastructure) | [`docs/system_overview_diagram.mmd`](../docs/system_overview_diagram.mmd) |
| SSO / Duende IdentityServer notes | [`docs/sso/`](../docs/sso/) |
| Architecture Decision Records | [`docs/adr/`](../docs/adr/) |

Put new architecture diagrams, component descriptions and detailed design in `/docs`, not here. Keep this file limited to project structure and how it maps to the architecture.

### Architecture Decision Records

- ADRs live in [`docs/adr/`](../docs/adr/), are numbered `NNNN-kebab-case-title.md`, and follow [`docs/adr/0000-template.md`](../docs/adr/0000-template.md).
- Write a new ADR whenever a change introduces or replaces a service, a layer boundary, a protocol, a storage engine, a hosting or deployment target, or a cross-cutting library choice (auth, messaging, observability).
- An accepted ADR is not edited to reverse it. Write a new ADR that supersedes it and set the old one's status to `Superseded by NNNN`.
- When a code change contradicts an existing ADR, flag it to the user before going ahead.

## Project structure

How each folder maps to the architecture layers in the system overview diagram:

| Path | Layer | Purpose | Status |
|---|---|---|---|
| `src/clients/web/chatty-duck-web` | Client | Web UI: React + TypeScript + Vite | Started |
| `src/clients/assets` | Client | Shared client assets (logos, banners) | Started |
| *(mobile client)* | Client | Mobile UI | Planned |
| `src/edge/ChattyDuck.Edge.Messenger` | Edge | Messenger: real-time chat over WebSocket (SignalR `ChatHub`), publishes to and consumes from RabbitMQ | Started |
| `src/edge/ChattyDuck.Edge.Notifier` | Edge | Notifier: pushes notifications to clients | Skeleton |
| *(REST proxy / API gateway)* | Edge | Single HTTP entry point: proxies SSO, reads from cache | Planned |
| `src/core/ChattyDuck.Core.SSO` | Core | Identity provider: Duende IdentityServer + ASP.NET Identity on PostgreSQL (EF Core migrations under `Data/Migrations`) | Started |
| *(Historical API)* | Core | Reads chat history from Storage, backed by a Redis cache | Planned |
| *(Hot Storage)* | Core | Consumes messages from RabbitMQ and persists them to Storage | Planned |
| `tools/ChattyDuck.Tools.ConsoleClient` | Tooling | Console client for manual testing of the Edge services | Started |
| `deploy/local` | Deployment | Local infrastructure via docker-compose (`compose.yaml`) | Started |
| `docs` | Docs | Architecture, diagrams, ADRs | Started |

Notes:
- `.NET` projects use the naming pattern `ChattyDuck.<Layer>.<Component>` and sit under `src/<layer>/`. Keep to that pattern for new services and add each one to `ChattyDuck.slnx` under the matching solution folder.
- When a planned component gets a folder, update this table in the same change.

### Shared build configuration

- `Directory.Build.props`: nullable, implicit usings, `AnalysisMode=All`, and analyzers (Meziantou, Sonar, Roslynator, xunit). Warnings are not errors yet; this will be tightened once the project leaves the early stage.
- `Directory.Packages.props`: central package management. Add package versions here, never in individual `.csproj` files.
- `.editorconfig`: coding style. Keep new code clean of analyzer warnings where practical.
- `.github/workflows/`: CI (backend PR build).
- Target framework: `net10.0`.

## Deployability

ChattyDuck must stay deployable to several environments, and **every change has to keep that true**. Early on there is some slack: a new service can land before its deployment wiring is complete. That gap must be visible, though (a TODO in the deployment files or a note in the PR), and closed soon after, never left to accumulate.

Target environments:

| Environment | Mechanism | Location |
|---|---|---|
| Local, developer inner loop | .NET Aspire AppHost | *planned* |
| Local, containers | docker-compose | `deploy/local/compose.yaml` |
| Local Kubernetes | minikube (or kind / k3d) with manifests or Helm | *planned*, under `deploy/` |
| Cloud | Azure (e.g. Container Apps / AKS) and GCP (e.g. Cloud Run / GKE) | *planned*, under `deploy/` |

Rules that keep this possible:
- Every runnable service has a Dockerfile (or uses .NET SDK container publishing) and a health check endpoint.
- Every service reads its dependencies (database, broker, cache, SSO authority) from configuration, with the same keys across all environments. Only the values differ.
- No cloud-vendor SDK in domain or service logic. If a cloud-specific service is used (for example Azure Service Bus instead of RabbitMQ), put it behind an abstraction and record the choice in an ADR.
- Secrets never go into the repository. Local defaults (such as the postgres password in `compose.yaml`) are for local development only.
- When you add or change a service or its dependencies, update every deployment target that already exists (Aspire, compose, k8s, cloud) in the same change, or record the gap as described above.

## Working on this repo

- Explain the reasoning behind non-obvious design choices. This is a learning project and the "why" is part of the deliverable.
- Keep changes scoped to one layer or component where possible. Changes that cross layer boundaries usually need an ADR.
- Write architecture diagrams in Mermaid (`.mmd`) under `/docs`, so they stay diffable and reviewable as text.

## Claude skills and agents

- `.claude/skills/` and `.claude/agents/` hold the Duende skills (IdentityServer, OAuth/OIDC, ASP.NET Core auth, BFF, token management) and two specialist agents (`identity-server-specialist`, `oauth-oidc-specialist`), vendored from the upstream Duende repo. Provenance and the update procedure are in [`skills/DUENDE-SOURCE.md`](skills/DUENDE-SOURCE.md).
- Use them for work on `ChattyDuck.Core.SSO` and for any service that authenticates against it.
- Don't edit the vendored files. Project-specific guidance goes in this file or in a new skill folder of our own.

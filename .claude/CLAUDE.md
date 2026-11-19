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
| *(edge proxy + API gateway, config under `deploy/shared/envoy`)* | Edge | Envoy: single public entry point and API gateway; TLS, path routing, WebSocket, rate limits, JWT validation and per-route scope checks; no login flows or sessions (ADR 0003, 0005) | Planned |
| *(BFF)* | Edge | Node.js (TypeScript) BFF for the web client: OIDC client, cookie sessions, CSRF; forwards `/bff/api` and `/bff/hubs` to Envoy's internal listener with bearer tokens (ADR 0003, 0005) | Planned |
| `src/core/ChattyDuck.Core.SSO` | Core | Identity provider: Duende IdentityServer + ASP.NET Identity on PostgreSQL (EF Core migrations under `Data/Migrations`) | Started |
| *(Historical API)* | Core | Reads chat history from Storage and owns its Redis cache; reached through Envoy at `/api/history` (ADR 0005) | Planned |
| *(Hot Storage)* | Core | Consumes messages from RabbitMQ and persists them to Storage | Planned |
| `tools/ChattyDuck.Tools.ConsoleClient` | Tooling | Console client for manual testing of the Edge services | Started |
| `deploy/local` | Deployment | Hand-written docker-compose (`compose.yaml`); replaced by Aspire-generated compose once it reaches parity (ADR 0004) | Started |
| `deploy/aspire/ChattyDuck.AppHost` | Deployment | Aspire AppHost: default inner loop and single source that generates compose and Helm output into `deploy/generated/` (ADR 0004). Models PostgreSQL, the SSO and the web client so far | Started |
| `docs` | Docs | Architecture, diagrams, ADRs | Started |

Notes:
- `.NET` projects use the naming pattern `ChattyDuck.<Layer>.<Component>` and sit under `src/<layer>/`. Keep to that pattern for new services and add each one to `ChattyDuck.slnx` under the matching solution folder.
- Aspire resources (and so compose services and Kubernetes Services) are named `chattyduck-<layer>-<service>`, for example `chattyduck-core-sso` or `chattyduck-storage-postgres`: the project name in lowercase with hyphens, because Aspire and DNS reject dots. Infrastructure follows it too, with its layer from the system overview diagram (ADR 0004).
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
| Local, developer inner loop (default) | .NET Aspire AppHost, run mode | `deploy/aspire/ChattyDuck.AppHost` (PostgreSQL, SSO and web client so far) |
| Local, containers | docker-compose, generated by `aspire publish` | `deploy/local/compose.yaml` (hand-written) until `deploy/generated/compose` reaches parity |
| Local Kubernetes | kind (or minikube / k3d) with the Helm chart generated by `aspire publish` | *planned*, no date; `deploy/generated/kubernetes` |
| Cloud | Kubernetes on Azure (AKS) and GCP (GKE), same Helm chart with per-cloud values; provisioning in a future ADR | *planned*, under `deploy/` |

ADR 0004 (Proposed) makes the AppHost the single source of every target: services are added there, and compose and Helm output is generated, committed, and never edited by hand. Services read only our own configuration keys (`docs/configuration.md`), never Aspire's service-discovery variables.

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

Third-party skills and agents are not copied into the repo. They are shared as Claude Code plugins through [`settings.json`](settings.json), which registers each plugin marketplace (`extraKnownMarketplaces`) and turns on the plugins this repo uses (`enabledPlugins`). Claude Code offers to install them when you trust the folder, and they follow upstream updates without manual copying.

- **Duende** ([DuendeSoftware/duende-skills](https://github.com/DuendeSoftware/duende-skills), plugin `duende-skills`): IdentityServer, OAuth/OIDC, ASP.NET Core auth, BFF and token-management skills, plus two specialist agents (`identity-server-specialist`, `oauth-oidc-specialist`). Use them for work on `ChattyDuck.Core.SSO` and for any service that authenticates against it.
- **.NET** ([dotnet/skills](https://github.com/dotnet/skills), marketplace `dotnet-agent-skills`): the plugins relevant to this repo are enabled (`dotnet`, `dotnet-aspnetcore`, `dotnet-data`, `dotnet-diag`, `dotnet-msbuild`, `dotnet-nuget`, `dotnet-test`). Some include an LSP server or MCP server, which copied skill files can't carry. To enable another plugin from that marketplace (for example `dotnet-upgrade`), add it to `enabledPlugins`.
- **Aspire** ([microsoft/aspire-skills](https://github.com/microsoft/aspire-skills), marketplace `aspire-skills`, plugin `aspire`): .NET Aspire AppHost skills. Use them for work on the AppHost (ADR 0004) and on the compose and Helm output it generates.
- Project-specific guidance goes in this file or in a skill folder of our own under `.claude/skills/`. Don't fork upstream plugin content into the repo.

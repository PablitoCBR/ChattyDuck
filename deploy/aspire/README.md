# Aspire AppHost (developer inner loop)

`ChattyDuck.AppHost` is the single description of the system (ADR 0004). In run mode it is the default way to run
ChattyDuck locally.

| Resource | Kind | Run mode | Publish mode |
|---|---|---|---|
| `chattyduck-storage-postgres` | PostgreSQL server, `postgres:18.7` container | Persistent container with the data volume `chatty-duck-aspire-postgres-18` | Container |
| `chattyduck-storage-identity-db` | Database `ChattyDuck_Identity` on `chattyduck-storage-postgres` | Created by Aspire if missing; the SSO applies its EF Core migrations | — |
| `chattyduck-core-sso` | `ChattyDuck.Core.SSO` | Host process (`dotnet run`), `https://localhost:5001` | Image built from `src/core/ChattyDuck.Core.SSO/Dockerfile` |

## Prerequisites

- .NET 10 SDK and the Aspire CLI (`aspire --version`).
- Docker Desktop running (PostgreSQL is a container).
- The trusted .NET development certificate (`dotnet dev-certs https --trust`), because the SSO serves HTTPS.

## Run

From anywhere in the repository (the root `aspire.config.json` points the CLI at the AppHost):

```bash
aspire run
```

Or in the background, then wait for the SSO:

```bash
aspire start
```

```bash
aspire wait chattyduck-core-sso
```

The CLI prints the dashboard URL. Check the SSO at <https://localhost:5001/.well-known/openid-configuration>.

Stop the compose stack (or a `dotnet run` SSO) first: they use port 5001 too.

## Why it is set up this way

- **Resource names follow `chattyduck-<layer>-<service>`** (ADR 0004). The name becomes the host name in generated
  compose and the Kubernetes Service name, so it uses hyphens: Aspire and DNS both reject dots. The hand-written
  `deploy/local/compose.yaml` keeps its short names (`postgres`, `sso`) until the generated file replaces it.
- **The SSO owns its configuration key.** The AppHost passes the database as `ConnectionStrings__Identity`
  (`connectionName: "Identity"`), the key the SSO reads in every target, instead of the SSO learning Aspire's names.
- **Aspire generates the PostgreSQL password** on the first run and keeps it outside the repository, in Aspire's
  per-user secrets store: `~/.aspire/secrets/<UserSecretsId>/Development.json`, key `Parameters:chattyduck-storage-postgres-password`
  (the `UserSecretsId` is in `ChattyDuck.AppHost.csproj`). It also remembers the host port PostgreSQL got
  (`Resources:chattyduck-storage-postgres:tcp:port`), so a database tool can keep using the same port. The easiest way to get a working
  connection string is the dashboard. Nothing is committed.
- **Persistent PostgreSQL container:** it keeps running after the AppHost stops, so restarts are fast. If that secrets
  file is deleted, Aspire generates a new password that no longer matches the data volume, and logins fail. Restore
  the old value, or remove the `chatty-duck-aspire-postgres-18` volume (this deletes the local SSO data).
- **The volume name carries the PostgreSQL major version.** A major version cannot open the previous one's data files
  (PostgreSQL 18 also moved its data folder), so a major upgrade gets a new volume, and migrating the old data is a
  deliberate step (`pg_upgrade` or a dump and restore).
- **Separate volume from compose:** the compose volume was created by PostgreSQL 15.3 with a different password, so the
  two cannot share it.

## Not done yet

- No compose or Kubernetes publisher yet, so `aspire publish` produces nothing. `aspire do build-chattyduck-core-sso` builds the SSO
  image.
- No health check for the SSO until it exposes `/health/live` and `/health/ready`.
- No Envoy yet; the SSO is reached directly on `https://localhost:5001`.

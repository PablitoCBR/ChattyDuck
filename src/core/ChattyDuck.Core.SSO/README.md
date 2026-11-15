# About

The `ChattyDuck.Core.SSO` project represents the Identity Provider services serving the SSO functionality for the Chatty Duck system.

# Setting-up from scratch
1. Ensure dotnet ef tools are installed `dotnet tool install --global dotnet-ef`.
2. Run ApplicationDbContext migration `dotnet ef migrations add InitialApplicationDbContext -c ApplicationDbContext -o Data/Migrations/ApplicationDbContext`
3. Run migration for persisted grants (operational data) `dotnet ef migrations add InitialPersistedGrantDbMigration -c PersistedGrantDbContext -o Data/Migrations/Operational`.
4. Run migration for configurational data `dotnet ef migrations add InitialIdentityServerConfigurationDbMigration -c ConfigurationDbContext -o Data/Migrations/Configuration`.
5. Run `dotnet ef database update -c <context>` for `ApplicationDbContext`, `PersistedGrantDbContext` and `ConfigurationDbContext`
# Open points

Gaps found in the best-practice review that are not addressed yet. Each one also has a `TODO` next to the code it concerns.

- [ ] **Decide where migrations and seeding run.** Today they run on every startup in every environment (`Configurations/Bootstrap.cs`).
- [ ] **Seeding never removes anything**, API scopes are always enabled, and identity resource claim casing is inconsistent (`Configurations/Bootstrap.cs`).
- [ ] **Read the Duende license key from configuration** and confirm the edition (`HostingExtensions.cs`).
- [ ] **Encrypt Data Protection keys at rest** outside local development (`HostingExtensions.cs`).
- [ ] **Health check endpoint**, then add it to the Dockerfile and compose (ADR 0002).
- [ ] **Run the container outside `Development`**: split the seed data out of `appsettings.Development.json` (`deploy/local/compose.yaml`).
- [ ] **Envoy in front of the SSO**: trust only Envoy, stop publishing the SSO ports, and configure Envoy as the edge for `X-Forwarded-*` (`deploy/local/compose.yaml`, ADR 0003).
- [ ] **Upgrade to Duende IdentityServer v8** (`Directory.Packages.props`).
- [ ] **Bump EF Core Tools** to get rid of the vulnerable `System.Security.Cryptography.Pkcs` 9.0.0 (design-time only) (`Directory.Packages.props`).
- [ ] **Integration tests**: discovery document, client-credentials round trip, audience in access tokens.
- [ ] **Fix this README:** step 2 of "Setting-up from scratch" says `Data/Migrations/ApplicationDbContext`, but the migrations live in `Data/Migrations/Identity`.

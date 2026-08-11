# About

The `ChattyDuck.Core.SSO` project represents the Identity Provider services serving the SSO functionality for the Chatty Duck system.

# Setting-up from scratch
1. Ensure dotnet ef tools are installed `dotnet tool install --global dotnet-ef`.
2. Run ApplicationDbContext migration `dotnet ef migrations add InitialApplicationDbContext -c ApplicationDbContext -o Data/Migrations/ApplicationDbContext`
3. Run migration for persisted grants (operational data) `dotnet ef migrations add InitialPersistedGrantDbMigration -c PersistedGrantDbContext -o Data/Migrations/Operational`.
4. Run migration for configurational data `dotnet ef migrations add InitialIdentityServerConfigurationDbMigration -c ConfigurationDbContext -o Data/Migrations/Configuration`.
5. Run `dotnet ef database update -c <context>` for `ApplicationDbContext`, `PersistedGrantDbContext` and `ConfigurationDbContext`
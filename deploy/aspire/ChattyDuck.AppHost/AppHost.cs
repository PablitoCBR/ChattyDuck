// ChattyDuck AppHost (ADR 0004): one model for `aspire run` and the generated compose/Helm output.
// - Resource names: chattyduck-<layer>-<service> (hyphens only; they become DNS host names).
// - Services read only their own config keys, so connection names are set explicitly below.
// - Secrets are Aspire parameters, stored outside the repo in ~/.aspire/secrets/<UserSecretsId>/.

var builder = DistributedApplication.CreateBuilder(args);

const string RepoRoot = "../../.."; // relative to this project folder

// ---- Storage ------------------------------------------------------------------------------------------------------

// Exact tag, set before WithDataVolume (the mount path depends on it due to difference between pg 17 and 18).
// The volume name carries the major version: a new major version needs a new volume or a data migration.
var postgres = builder.AddPostgres("chattyduck-storage-postgres")
    .WithImageTag("18.7")
    .WithDataVolume("chatty-duck-aspire-postgres-18")
    .WithLifetime(ContainerLifetime.Persistent);

// Created by Aspire if missing; the SSO applies its EF Core migrations on startup.
var identityDb = postgres.AddDatabase("chattyduck-storage-identity-db", databaseName: "ChattyDuck_Identity");

// ---- Core ---------------------------------------------------------------------------------------------------------

// Run mode: host process on https://localhost:5001 (from its launchSettings.json).
// Publish mode: image from the SSO's Dockerfile, built from the repository root.
var sso = builder.AddDotnetProject("chattyduck-core-sso", $"{RepoRoot}/src/core/ChattyDuck.Core.SSO/ChattyDuck.Core.SSO.csproj")
    .WithReference(identityDb, connectionName: "Identity") // the SSO reads ConnectionStrings:Identity
    .WaitFor(identityDb)
    .PublishAsDockerFile(container => container.WithDockerfile(
        contextPath: RepoRoot,
        dockerfilePath: "src/core/ChattyDuck.Core.SSO/Dockerfile"));

// TODO: .WithHttpHealthCheck("/health/ready") once the SSO has health endpoints (ADR 0002, 0004).
// TODO: Move the SSO behind Envoy on fixed HTTP port 5101 (ADR 0003, 0005).

builder.Build().Run();

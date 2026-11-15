using Duende.IdentityServer;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using HashExtensions = Duende.IdentityServer.Models.HashExtensions;

namespace ChattyDuck.Core.SSO.Configurations;

public static partial class Bootstrap
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1873:Avoid potentially expensive logging", Justification = "Startup class is not performance critical.")]
    public class Clients
    {
        public static void Configure(IServiceProvider serviceProvider, Configuration configuration)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Clients>>();
            var context = serviceProvider.GetRequiredService<ConfigurationDbContext>();

            foreach (var clientConfiguration in configuration.Clients)
            {
                if (string.IsNullOrWhiteSpace(clientConfiguration.ClientId))
                {
                    logger.LogWarning("Skipping client because its ClientId is empty.");
                    continue;
                }

                if (clientConfiguration.RequireClientSecret && !clientConfiguration.ClientSecrets.Any(secret => !string.IsNullOrWhiteSpace(secret.Value)))
                {
                    // A confidential client without a secret can never authenticate at the token endpoint (invalid_client).
                    // Fail loudly at startup instead of at the first login attempt.
                    logger.LogError("Skipping client {ClientId} because it requires a client secret but none is configured.", clientConfiguration.ClientId);
                    continue;
                }

                var existingClient = context.Clients
                    .Include(client => client.AllowedGrantTypes)
                    .Include(client => client.RedirectUris)
                    .Include(client => client.PostLogoutRedirectUris)
                    .Include(client => client.AllowedScopes)
                    .Include(client => client.AllowedCorsOrigins)
                    .Include(client => client.ClientSecrets)
                    .SingleOrDefault(client => client.ClientId == clientConfiguration.ClientId);

                if (existingClient is null)
                {
                    logger.LogInformation("Creating client {ClientId}.", clientConfiguration.ClientId);

                    var client = new Client
                    {
                        ClientId = clientConfiguration.ClientId,
                        AllowedGrantTypes = [.. clientConfiguration.AllowedGrantTypes
                            .Where(grantType => !string.IsNullOrWhiteSpace(grantType))
                            .Select(grantType => new ClientGrantType { GrantType = grantType })],
                        RedirectUris = [.. clientConfiguration.RedirectUris
                            .Where(uri => !string.IsNullOrWhiteSpace(uri))
                            .Select(uri => new ClientRedirectUri { RedirectUri = uri })],
                        PostLogoutRedirectUris = [.. clientConfiguration.PostLogoutRedirectUris
                            .Where(uri => !string.IsNullOrWhiteSpace(uri))
                            .Select(uri => new ClientPostLogoutRedirectUri { PostLogoutRedirectUri = uri })],
                        AllowedScopes = [.. clientConfiguration.AllowedScopes
                            .Where(scope => !string.IsNullOrWhiteSpace(scope))
                            .Select(scope => new ClientScope { Scope = scope })],
                        AllowedCorsOrigins = [.. clientConfiguration.AllowedCorsOrigins
                            .Where(origin => !string.IsNullOrWhiteSpace(origin))
                            .Select(origin => new ClientCorsOrigin { Origin = origin })],
                        ClientSecrets = [.. clientConfiguration.ClientSecrets
                            .Where(secret => !string.IsNullOrWhiteSpace(secret.Value))
                            .Select(ToEntity)]
                    };

                    // Scalar settings share one code path with updates, so "create" and "update" can never drift apart.
                    UpdateScalarSettings(client, clientConfiguration, logger: null);

                    context.Clients.Add(client);
                }
                else
                {
                    var updated = UpdateExistingClientIfDifferent(existingClient, clientConfiguration, logger);
                    logger.LogInformation("Client {ClientId} {Result}.", clientConfiguration.ClientId, updated ? "updated" : "already up to date");
                }
            }

            context.SaveChanges();
        }

        private static bool UpdateExistingClientIfDifferent(Client existingClient, ConfigurationClient clientConfiguration, ILogger logger)
        {
            var updated = UpdateScalarSettings(existingClient, clientConfiguration, logger);

            updated |= SyncCollection(
                existingClient.ClientId, "allowed grant types", logger,
                existingClient.AllowedGrantTypes, clientConfiguration.AllowedGrantTypes,
                entity => entity.GrantType, value => new ClientGrantType { GrantType = value });

            updated |= SyncCollection(
                existingClient.ClientId, "redirect URIs", logger,
                existingClient.RedirectUris, clientConfiguration.RedirectUris,
                entity => entity.RedirectUri, value => new ClientRedirectUri { RedirectUri = value });

            updated |= SyncCollection(
                existingClient.ClientId, "post-logout redirect URIs", logger,
                existingClient.PostLogoutRedirectUris, clientConfiguration.PostLogoutRedirectUris,
                entity => entity.PostLogoutRedirectUri, value => new ClientPostLogoutRedirectUri { PostLogoutRedirectUri = value });

            updated |= SyncCollection(
                existingClient.ClientId, "allowed scopes", logger,
                existingClient.AllowedScopes, clientConfiguration.AllowedScopes,
                entity => entity.Scope, value => new ClientScope { Scope = value });

            updated |= SyncCollection(
                existingClient.ClientId, "allowed CORS origins", logger,
                existingClient.AllowedCorsOrigins, clientConfiguration.AllowedCorsOrigins,
                entity => entity.Origin, value => new ClientCorsOrigin { Origin = value });

            updated |= SyncSecrets(existingClient, clientConfiguration, logger);

            return updated;
        }

        /// <summary>
        /// Applies every single-value setting from configuration. Pass <paramref name="logger"/> = null when creating a client,
        /// where reporting "changed from default to X" would only be noise.
        /// </summary>
        private static bool UpdateScalarSettings(Client client, ConfigurationClient configuration, ILogger? logger)
        {
            var updated = false;

            updated |= Set(client, logger, nameof(Client.Enabled), client.Enabled, configuration.Enabled, value => client.Enabled = value);
            updated |= Set(client, logger, nameof(Client.ClientName), client.ClientName, configuration.ClientName, value => client.ClientName = value);
            updated |= Set(client, logger, nameof(Client.Description), client.Description, configuration.Description, value => client.Description = value);
            updated |= Set(client, logger, nameof(Client.RequireClientSecret), client.RequireClientSecret, configuration.RequireClientSecret, value => client.RequireClientSecret = value);
            updated |= Set(client, logger, nameof(Client.RequirePkce), client.RequirePkce, configuration.RequirePkce, value => client.RequirePkce = value);
            updated |= Set(client, logger, nameof(Client.AllowOfflineAccess), client.AllowOfflineAccess, configuration.AllowOfflineAccess, value => client.AllowOfflineAccess = value);
            updated |= Set(client, logger, nameof(Client.AccessTokenLifetime), client.AccessTokenLifetime, configuration.AccessTokenLifetime, value => client.AccessTokenLifetime = value);
            updated |= Set(client, logger, nameof(Client.IdentityTokenLifetime), client.IdentityTokenLifetime, configuration.IdentityTokenLifetime, value => client.IdentityTokenLifetime = value);
            updated |= Set(client, logger, nameof(Client.AbsoluteRefreshTokenLifetime), client.AbsoluteRefreshTokenLifetime, configuration.AbsoluteRefreshTokenLifetime, value => client.AbsoluteRefreshTokenLifetime = value);
            updated |= Set(client, logger, nameof(Client.SlidingRefreshTokenLifetime), client.SlidingRefreshTokenLifetime, configuration.SlidingRefreshTokenLifetime, value => client.SlidingRefreshTokenLifetime = value);
            // The EF entity stores these enums as plain ints.
            updated |= Set(client, logger, nameof(Client.RefreshTokenUsage), client.RefreshTokenUsage, (int)configuration.RefreshTokenUsage, value => client.RefreshTokenUsage = value);
            updated |= Set(client, logger, nameof(Client.RefreshTokenExpiration), client.RefreshTokenExpiration, (int)configuration.RefreshTokenExpiration, value => client.RefreshTokenExpiration = value);

            return updated;
        }

        private static bool Set<T>(Client client, ILogger? logger, string property, T current, T desired, Action<T> apply)
        {
            if (EqualityComparer<T>.Default.Equals(current, desired))
            {
                return false;
            }

            logger?.LogInformation("Updating {Property} for {ClientId} from {OldValue} to {NewValue}.", property, client.ClientId, current, desired);
            apply(desired);
            return true;
        }

        /// <summary>
        /// Makes a child collection of the client match the configured values: removes extra rows and adds missing ones.
        /// </summary>
        private static bool SyncCollection<TEntity>(
            string clientId,
            string description,
            ILogger logger,
            List<TEntity> existing,
            IEnumerable<string> configured,
            Func<TEntity, string> getValue,
            Func<string, TEntity> create)
        {
            var desiredValues = configured.Where(value => !string.IsNullOrWhiteSpace(value)).ToHashSet();
            var existingValues = existing.Select(getValue).ToHashSet();

            if (desiredValues.SetEquals(existingValues))
            {
                return false;
            }

            var toRemove = existingValues.Except(desiredValues).ToHashSet();
            var toAdd = desiredValues.Except(existingValues).ToList();
            logger.LogInformation("Updating {Description} for {ClientId}. Removing: {Removed}, Adding: {Added}.", description, clientId, string.Join(", ", toRemove), string.Join(", ", toAdd));

            existing.RemoveAll(entity => toRemove.Contains(getValue(entity)));
            existing.AddRange(toAdd.Select(create));
            return true;
        }

        /// <summary>
        /// Secrets are compared by hash, because only the hash is stored. Values are never logged.
        /// </summary>
        private static bool SyncSecrets(Client existingClient, ConfigurationClient clientConfiguration, ILogger logger)
        {
            var desired = clientConfiguration.ClientSecrets
                .Where(secret => !string.IsNullOrWhiteSpace(secret.Value))
                .Select(ToEntity)
                .ToList();

            static string Key(ClientSecret secret) => $"{secret.Type}|{secret.Value}|{secret.Expiration:O}|{secret.Description}";

            var desiredKeys = desired.Select(Key).ToHashSet();
            var existingKeys = existingClient.ClientSecrets.Select(Key).ToHashSet();

            if (desiredKeys.SetEquals(existingKeys))
            {
                return false;
            }

            var removed = existingClient.ClientSecrets.RemoveAll(secret => !desiredKeys.Contains(Key(secret)));
            var toAdd = desired.Where(secret => !existingKeys.Contains(Key(secret))).ToList();
            existingClient.ClientSecrets.AddRange(toAdd);

            logger.LogInformation("Updating client secrets for {ClientId}. Removed: {RemovedCount}, Added: {AddedCount}.", existingClient.ClientId, removed, toAdd.Count);
            return true;
        }

        private static ClientSecret ToEntity(ConfigurationClientSecret secret) => new()
        {
            // IdentityServer stores and compares shared secrets as hashes, the same way a password store would.
            Value = HashExtensions.Sha256(secret.Value),
            Type = IdentityServerConstants.SecretTypes.SharedSecret,
            Description = secret.Description,
            Expiration = secret.Expiration,
        };
    }
}

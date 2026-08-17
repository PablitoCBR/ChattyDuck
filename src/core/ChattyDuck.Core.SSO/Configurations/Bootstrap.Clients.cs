using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;

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

                var existingClient = context.Clients
                    .Include(client => client.AllowedGrantTypes)
                    .Include(client => client.RedirectUris)
                    .Include(client => client.PostLogoutRedirectUris)
                    .Include(client => client.AllowedScopes)
                    .SingleOrDefault(client => client.ClientId == clientConfiguration.ClientId);

                if (existingClient is null)
                {
                    logger.LogInformation("Creating client {ClientId}.", clientConfiguration.ClientId);

                    var client = new Client
                    {
                        Enabled = clientConfiguration.Enabled,
                        ClientId = clientConfiguration.ClientId,
                        ClientName = clientConfiguration.ClientName,
                        Description = clientConfiguration.Description,
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
                            .Select(scope => new ClientScope { Scope = scope! })]
                    };

                    context.Clients.Add(client);
                }
                else
                {
                    var updated = UpdateExistingClientIfDifferent(context, existingClient, clientConfiguration, logger);
                    if (updated)
                    {
                        context.SaveChanges();
                        logger.LogInformation("Client {ClientId} updated.", clientConfiguration.ClientId);
                    }
                    else
                    {
                        logger.LogInformation("Client {ClientId} already exists.", clientConfiguration.ClientId);
                    }
                }
            }

            context.SaveChanges();
        }

        private static bool UpdateExistingClientIfDifferent(ConfigurationDbContext context, Client existingClient, ConfigurationClient clientConfiguration, ILogger<Clients> logger)
        {
            var updated = false;

            if (existingClient.Enabled != clientConfiguration.Enabled)
            {
                logger.LogInformation("Updating enabled status for {ClientId} from {OldEnabled} to {NewEnabled}.", existingClient.ClientId, existingClient.Enabled, clientConfiguration.Enabled);
                existingClient.Enabled = clientConfiguration.Enabled;
                updated = true;
            }

            if (existingClient.ClientName != clientConfiguration.ClientName)
            {
                logger.LogInformation("Updating client name for {ClientId} from {OldClientName} to {NewClientName}.", existingClient.ClientId, existingClient.ClientName, clientConfiguration.ClientName);
                existingClient.ClientName = clientConfiguration.ClientName;
                updated = true;
            }

            if (existingClient.Description != clientConfiguration.Description)
            {
                logger.LogInformation("Updating description for {ClientId} from {OldDescription} to {NewDescription}.", existingClient.ClientId, existingClient.Description, clientConfiguration.Description);
                existingClient.Description = clientConfiguration.Description;
                updated = true;
            }

            // Update AllowedGrantTypes
            var desiredGrantTypes = clientConfiguration.AllowedGrantTypes.Where(gt => !string.IsNullOrWhiteSpace(gt)).ToHashSet();
            var existingGrantTypes = existingClient.AllowedGrantTypes.Select(gt => gt.GrantType).ToHashSet();

            if (!desiredGrantTypes.SetEquals(existingGrantTypes))
            {
                var grantTypesToRemove = existingGrantTypes.Except(desiredGrantTypes).ToHashSet();
                var grantTypesToAdd = desiredGrantTypes.Except(existingGrantTypes).ToList();
                logger.LogInformation("Updating allowed grant types for {ClientId}. Removing: {RemovedGrantTypes}, Adding: {AddedGrantTypes}.", existingClient.ClientId, string.Join(", ", grantTypesToRemove), string.Join(", ", grantTypesToAdd));

                existingClient.AllowedGrantTypes.Where(gt => grantTypesToRemove.Contains(gt.GrantType)).ToList().ForEach(gt => existingClient.AllowedGrantTypes.Remove(gt));
                existingClient.AllowedGrantTypes.AddRange(grantTypesToAdd.Select(gt => new ClientGrantType { GrantType = gt }));

                updated = true;
            }

            // Update RedirectUris
            var desiredRedirectUris = clientConfiguration.RedirectUris.Where(uri => !string.IsNullOrWhiteSpace(uri)).ToHashSet();
            var existingRedirectUris = existingClient.RedirectUris.Select(uri => uri.RedirectUri).ToHashSet();

            if (!desiredRedirectUris.SetEquals(existingRedirectUris))
            {
                var redirectUrisToRemove = existingRedirectUris.Except(desiredRedirectUris).ToHashSet();
                var redirectUrisToAdd = desiredRedirectUris.Except(existingRedirectUris).ToList();
                logger.LogInformation("Updating redirect URIs for {ClientId}. Removing: {RemovedRedirectUris}, Adding: {AddedRedirectUris}.", existingClient.ClientId, string.Join(", ", redirectUrisToRemove), string.Join(", ", redirectUrisToAdd));

                existingClient.RedirectUris.Where(r => redirectUrisToRemove.Contains(r.RedirectUri)).ToList().ForEach(r => existingClient.RedirectUris.Remove(r));
                existingClient.RedirectUris.AddRange(redirectUrisToAdd.Select(uri => new ClientRedirectUri { RedirectUri = uri }));

                updated = true;
            }

            // Update PostLogoutRedirectUris
            var desiredPostLogoutRedirectUris = clientConfiguration.PostLogoutRedirectUris.Where(uri => !string.IsNullOrWhiteSpace(uri)).ToHashSet();
            var existingPostLogoutRedirectUris = existingClient.PostLogoutRedirectUris.Select(uri => uri.PostLogoutRedirectUri).ToHashSet();

            if (!desiredPostLogoutRedirectUris.SetEquals(existingPostLogoutRedirectUris))
            {
                var postLogoutRedirectUrisToRemove = existingPostLogoutRedirectUris.Except(desiredPostLogoutRedirectUris).ToHashSet();
                var postLogoutRedirectUrisToAdd = desiredPostLogoutRedirectUris.Except(existingPostLogoutRedirectUris).ToList();
                logger.LogInformation("Updating post-logout redirect URIs for {ClientId}. Removing: {RemovedPostLogoutRedirectUris}, Adding: {AddedPostLogoutRedirectUris}.", existingClient.ClientId, string.Join(", ", postLogoutRedirectUrisToRemove), string.Join(", ", postLogoutRedirectUrisToAdd));

                existingClient.PostLogoutRedirectUris.Where(r => postLogoutRedirectUrisToRemove.Contains(r.PostLogoutRedirectUri)).ToList().ForEach(r => existingClient.PostLogoutRedirectUris.Remove(r));
                existingClient.PostLogoutRedirectUris.AddRange(postLogoutRedirectUrisToAdd.Select(uri => new ClientPostLogoutRedirectUri { PostLogoutRedirectUri = uri }));

                updated = true;
            }

            // Update AllowedScopes
            var desiredScopes = clientConfiguration.AllowedScopes.Where(scope => !string.IsNullOrWhiteSpace(scope)).ToHashSet();
            var existingAllowedScopes = existingClient.AllowedScopes.Select(scope => scope.Scope).ToHashSet();

            if (!desiredScopes.SetEquals(existingAllowedScopes))
            {
                var scopesToRemove = existingAllowedScopes.Except(desiredScopes).ToHashSet();
                var scopesToAdd = desiredScopes.Except(existingAllowedScopes).ToList();
                logger.LogInformation("Updating allowed scopes for {ClientId}. Removing: {RemovedScopes}, Adding: {AddedScopes}.", existingClient.ClientId, string.Join(", ", scopesToRemove), string.Join(", ", scopesToAdd));

                existingClient.AllowedScopes.Where(s => scopesToRemove.Contains(s.Scope)).ToList().ForEach(s => existingClient.AllowedScopes.Remove(s));
                existingClient.AllowedScopes.AddRange(scopesToAdd.Select(scope => new ClientScope { Scope = scope }));

                updated = true;
            }

            return updated;
        }
    }
}
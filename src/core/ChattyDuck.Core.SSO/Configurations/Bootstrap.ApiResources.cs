using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;

namespace ChattyDuck.Core.SSO.Configurations;

public static partial class Bootstrap
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1873:Avoid potentially expensive logging", Justification = "Startup class is not performance critical.")]
    public class ApiResources
    {
        public static void Configure(IServiceProvider serviceProvider, Configuration configuration)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<ApiResources>>();
            var context = serviceProvider.GetRequiredService<ConfigurationDbContext>();

            foreach (var resourceConfiguration in configuration.ApiResources)
            {
                if (string.IsNullOrWhiteSpace(resourceConfiguration.Name))
                {
                    logger.LogWarning("Skipping API resource because its name is empty.");
                    continue;
                }

                var unmatchedScopes = resourceConfiguration.Scopes
                    .Where(scope => !string.IsNullOrWhiteSpace(scope))
                    .Where(scope => !context.ApiScopes.Any(apiScope => apiScope.Name == scope))
                    .ToList();

                if (unmatchedScopes.Count != 0)
                {
                    logger.LogError("Skipping API resource {Name} because the following scopes do not exist: {Scopes}.", resourceConfiguration.Name, string.Join(", ", unmatchedScopes));
                    continue;
                }

                var existingResource = context.ApiResources.SingleOrDefault(resource => resource.Name == resourceConfiguration.Name);

                if (existingResource is null)
                {
                    logger.LogInformation("Creating API resource {Name}.", resourceConfiguration.Name);

                    var resource = new ApiResource
                    {
                        Enabled = resourceConfiguration.Enabled,
                        Name = resourceConfiguration.Name,
                        DisplayName = resourceConfiguration.DisplayName,
                        Description = resourceConfiguration.Description,
                        Scopes = [.. resourceConfiguration.Scopes
                            .Where(scope => !string.IsNullOrWhiteSpace(scope))
                            .Select(scope => new ApiResourceScope { Scope = scope! })],
                        UserClaims = [.. resourceConfiguration.UserClaims
                            .Where(claim => !string.IsNullOrWhiteSpace(claim))
                            .Select(claim => new ApiResourceClaim { Type = claim! })]
                    };

                    context.ApiResources.Add(resource);
                }
                else
                {
                    var updated = UpdateExistingResourceIfDifferent(context, existingResource, resourceConfiguration, logger);

                    if (updated)
                    {
                        context.SaveChanges();
                        logger.LogInformation("API resource {Name} updated.", resourceConfiguration.Name);
                    }
                    else
                    {
                        logger.LogInformation("API resource {Name} already exists.", resourceConfiguration.Name);
                    }
                }

            }

            context.SaveChanges();
        }

        private static bool UpdateExistingResourceIfDifferent(ConfigurationDbContext context, ApiResource existingResource, ConfigurationApiResource resourceConfiguration, ILogger<ApiResources> logger)
        {
            var updated = false;

            if (existingResource.Enabled != resourceConfiguration.Enabled)
            {
                logger.LogInformation("Updating enabled status for API resource {Name} from {OldEnabled} to {NewEnabled}.", existingResource.Name, existingResource.Enabled, resourceConfiguration.Enabled);
                existingResource.Enabled = resourceConfiguration.Enabled;
                updated = true;
            }

            if (existingResource.DisplayName != resourceConfiguration.DisplayName)
            {
                logger.LogInformation("Updating display name for API resource {Name} from {OldDisplayName} to {NewDisplayName}.", existingResource.Name, existingResource.DisplayName, resourceConfiguration.DisplayName);
                existingResource.DisplayName = resourceConfiguration.DisplayName;
                updated = true;
            }

            if (existingResource.Description != resourceConfiguration.Description)
            {
                logger.LogInformation("Updating description for API resource {Name} from {OldDescription} to {NewDescription}.", existingResource.Name, existingResource.Description, resourceConfiguration.Description);
                existingResource.Description = resourceConfiguration.Description;
                updated = true;
            }

            var existingScopes = existingResource.Scopes.Select(s => s.Scope).ToHashSet();
            var desiredScopes = resourceConfiguration.Scopes.Where(scope => !string.IsNullOrWhiteSpace(scope)).ToHashSet();

            if (!existingScopes.SetEquals(desiredScopes))
            {
                var scopesToRemove = existingScopes.Except(desiredScopes).ToHashSet();
                var scopesToAdd = desiredScopes.Except(existingScopes).ToList();

                logger.LogInformation("Updating scopes for API resource {Name}. Scopes to remove: {ScopesToRemove}, Scopes to add: {ScopesToAdd}.", existingResource.Name, string.Join(", ", scopesToRemove), string.Join(", ", scopesToAdd));

                existingResource.Scopes.Where(s => scopesToRemove.Contains(s.Scope)).ToList().ForEach(s => existingResource.Scopes.Remove(s));
                existingResource.Scopes.AddRange(scopesToAdd.Select(scope => new ApiResourceScope { Scope = scope }));
                
                updated = true;
            }

            var existingClaims = existingResource.UserClaims.Select(c => c.Type).ToHashSet();
            var desiredClaims = resourceConfiguration.UserClaims.Where(claim => !string.IsNullOrWhiteSpace(claim)).ToHashSet();

            if (!existingClaims.SetEquals(desiredClaims))
            {
                var claimsToRemove = existingClaims.Except(desiredClaims).ToHashSet();
                var claimsToAdd = desiredClaims.Except(existingClaims).ToList();

                logger.LogInformation("Updating user claims for API resource {Name}. Claims to remove: {ClaimsToRemove}, Claims to add: {ClaimsToAdd}.", existingResource.Name, string.Join(", ", claimsToRemove), string.Join(", ", claimsToAdd));
                existingResource.UserClaims.Where(c => claimsToRemove.Contains(c.Type)).ToList().ForEach(c => existingResource.UserClaims.Remove(c));
                existingResource.UserClaims.AddRange(claimsToAdd.Select(claim => new ApiResourceClaim { Type = claim }));

                updated = true;
            }

            return updated;
        }
    }
}

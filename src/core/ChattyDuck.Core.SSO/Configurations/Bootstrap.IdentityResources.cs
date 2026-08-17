using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChattyDuck.Core.SSO.Configurations;

public static partial class Bootstrap
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1873:Avoid potentially expensive logging", Justification = "Startup class is not performance critical.")]
    public class IdentityResources
    {
        public static void Configure(IServiceProvider serviceProvider, Configuration configuration)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<IdentityResources>>();
            using var context = serviceProvider.GetRequiredService<ConfigurationDbContext>();

            foreach (var resourceConfiguration in configuration.IdentityResources)
            {
                if (string.IsNullOrWhiteSpace(resourceConfiguration.Name))
                {
                    logger.LogWarning("Skipping identity resource because its name is empty.");
                    continue;
                }

                var existingResource = context.IdentityResources
                    .Include(resource => resource.UserClaims)
                    .SingleOrDefault(resource => resource.Name == resourceConfiguration.Name);

                if (existingResource is null)
                {
                    logger.LogInformation("Creating identity resource {Name}.", resourceConfiguration.Name);

                    var resource = new IdentityResource
                    {
                        Enabled = resourceConfiguration.Enabled,
                        Name = resourceConfiguration.Name,
                        DisplayName = resourceConfiguration.DisplayName,
                        Description = resourceConfiguration.Description,
                        Required = resourceConfiguration.Required,
                        Emphasize = resourceConfiguration.Emphasize,
                        UserClaims = resourceConfiguration.UserClaims
                            .Where(claim => !string.IsNullOrWhiteSpace(claim))
                            .Select(claim => new IdentityResourceClaim { Type = claim! })
                            .ToList()
                    };

                    context.IdentityResources.Add(resource);
                }
                else
                {
                    var updated = UpdateExistingResourceIfDifferent(context, existingResource, resourceConfiguration, logger);

                    if (updated)
                    {
                        context.SaveChanges();
                        logger.LogInformation("Identity resource {Name} updated.", resourceConfiguration.Name);
                    }
                    else
                    {
                        logger.LogInformation("Identity resource {Name} already exists.", resourceConfiguration.Name);
                    }
                }
            }

            context.SaveChanges();
        }

        private static bool UpdateExistingResourceIfDifferent(ConfigurationDbContext context, IdentityResource existingResource, ConfigurationIdentityResource resourceConfiguration, ILogger logger)
        {
            var updated = false;

            if (existingResource.Enabled != resourceConfiguration.Enabled)
            {
                logger.LogInformation("Updating identity resource {Name}: Enabled changed from {OldValue} to {NewValue}.", existingResource.Name, existingResource.Enabled, resourceConfiguration.Enabled);
                existingResource.Enabled = resourceConfiguration.Enabled;
                updated = true;
            }

            if ((existingResource.DisplayName ?? string.Empty) != (resourceConfiguration.DisplayName ?? string.Empty))
            {
                logger.LogInformation("Updating identity resource {Name}: DisplayName changed from {OldValue} to {NewValue}.", existingResource.Name, existingResource.DisplayName, resourceConfiguration.DisplayName);
                existingResource.DisplayName = resourceConfiguration.DisplayName;
                updated = true;
            }

            if ((existingResource.Description ?? string.Empty) != (resourceConfiguration.Description ?? string.Empty))
            {
                logger.LogInformation("Updating identity resource {Name}: Description changed from {OldValue} to {NewValue}.", existingResource.Name, existingResource.Description, resourceConfiguration.Description);
                existingResource.Description = resourceConfiguration.Description;
                updated = true;
            }

            if (existingResource.Required != resourceConfiguration.Required)
            {
                logger.LogInformation("Updating identity resource {Name}: Required changed from {OldValue} to {NewValue}.", existingResource.Name, existingResource.Required, resourceConfiguration.Required);
                existingResource.Required = resourceConfiguration.Required;
                updated = true;
            }

            if (existingResource.Emphasize != resourceConfiguration.Emphasize)
            {
                logger.LogInformation("Updating identity resource {Name}: Emphasize changed from {OldValue} to {NewValue}.", existingResource.Name, existingResource.Emphasize, resourceConfiguration.Emphasize);
                existingResource.Emphasize = resourceConfiguration.Emphasize;
                updated = true;
            }

            // Normalize desired claims
            var desiredClaims = resourceConfiguration.UserClaims
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c!.Trim().ToLowerInvariant())
                .ToHashSet();

            var currentClaims = existingResource.UserClaims
                .Select(claim => claim.Type)
                .ToHashSet();

            if (desiredClaims.SetEquals(currentClaims))
            {
                return updated; // No changes needed for claims
            }

            var addedClaims = desiredClaims.Except(currentClaims).ToList();
            var removedClaims = currentClaims.Except(desiredClaims).ToHashSet();

            logger.LogInformation("Updating identity resource {Name}: Claims changed. Added: {AddedClaims}, Removed: {RemovedClaims}.", existingResource.Name, string.Join(", ", addedClaims), string.Join(", ", removedClaims));

            existingResource.UserClaims.Where(c => removedClaims.Contains(c.Type)).ToList().ForEach(c => existingResource.UserClaims.Remove(c));
            existingResource.UserClaims.AddRange(addedClaims.Select(claim => new IdentityResourceClaim { Type = claim }));

            return true;
        }
    }
}
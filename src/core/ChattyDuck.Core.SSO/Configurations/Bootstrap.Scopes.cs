using System;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChattyDuck.Core.SSO.Configurations;

public static partial class Bootstrap
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1873:Avoid potentially expensive logging", Justification = "Startup class is not performance critical.")]
    public class Scopes
    {
        public static void Configure(IServiceProvider serviceProvider, Configuration configuration)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Scopes>>();
            using var context = serviceProvider.GetRequiredService<ConfigurationDbContext>();

            foreach (var scopeConfiguration in configuration.Scopes)
            {
                if (string.IsNullOrWhiteSpace(scopeConfiguration.Name))
                {
                    logger.LogWarning("Skipping API scope because its name is empty.");
                    continue;
                }

                var existingScope = context.ApiScopes
                    .Include(scope => scope.UserClaims)
                    .SingleOrDefault(scope => scope.Name == scopeConfiguration.Name);

                if (existingScope is null)
                {
                    logger.LogInformation("Creating API scope {Name}.", scopeConfiguration.Name);

                    var scope = new ApiScope
                    {
                        Enabled = true,
                        Name = scopeConfiguration.Name,
                        DisplayName = scopeConfiguration.DisplayName,
                        Description = scopeConfiguration.Description,
                        Required = scopeConfiguration.Required,
                        Emphasize = scopeConfiguration.Emphasize,
                        UserClaims = [.. scopeConfiguration.UserClaims
                            .Where(claim => !string.IsNullOrWhiteSpace(claim))
                            .Select(claim => new ApiScopeClaim { Type = claim! })]
                    };

                    context.ApiScopes.Add(scope);
                }
                else
                {
                    var updated = UpdateExistingScopeIfDifferent(context, existingScope, scopeConfiguration, logger);

                    if (updated)
                    {
                        context.SaveChanges();
                        logger.LogInformation("API scope {Name} updated.", scopeConfiguration.Name);
                    }
                    else
                    {
                        logger.LogInformation("API scope {Name} already exists.", scopeConfiguration.Name);
                    }
                }
            }

            context.SaveChanges();
        }

        private static bool UpdateExistingScopeIfDifferent(ConfigurationDbContext context, ApiScope existingScope, ConfigurationScope scopeConfiguration, ILogger<Scopes> logger)
        {
            var updated = false;

            if (existingScope.DisplayName != scopeConfiguration.DisplayName)
            {
                logger.LogInformation("Updating display name for API scope {Name} from {OldDisplayName} to {NewDisplayName}.", existingScope.Name, existingScope.DisplayName, scopeConfiguration.DisplayName);
                existingScope.DisplayName = scopeConfiguration.DisplayName;
                updated = true;
            }

            if (existingScope.Description != scopeConfiguration.Description)
            {
                logger.LogInformation("Updating description for API scope {Name} from {OldDescription} to {NewDescription}.", existingScope.Name, existingScope.Description, scopeConfiguration.Description);
                existingScope.Description = scopeConfiguration.Description;
                updated = true;
            }

            if (existingScope.Required != scopeConfiguration.Required)
            {
                logger.LogInformation("Updating required flag for API scope {Name} from {OldRequired} to {NewRequired}.", existingScope.Name, existingScope.Required, scopeConfiguration.Required);
                existingScope.Required = scopeConfiguration.Required;
                updated = true;
            }

            if (existingScope.Emphasize != scopeConfiguration.Emphasize)
            {
                logger.LogInformation("Updating emphasize flag for API scope {Name} from {OldEmphasize} to {NewEmphasize}.", existingScope.Name, existingScope.Emphasize, scopeConfiguration.Emphasize);
                existingScope.Emphasize = scopeConfiguration.Emphasize;
                updated = true;
            }

            var existingClaims = existingScope.UserClaims.Select(c => c.Type).ToHashSet();
            var desiredClaims = scopeConfiguration.UserClaims.Where(c => !string.IsNullOrWhiteSpace(c)).ToHashSet();

            if (existingClaims.SetEquals(desiredClaims))
            {
                return updated;
            }

            var claimsToAdd = desiredClaims.Except(existingClaims).ToList();
            var claimsToRemove = existingClaims.Except(desiredClaims).ToList();

            logger.LogInformation("API scope {Name} claims to add: {ClaimsToAdd}, claims to remove: {ClaimsToRemove}.", existingScope.Name, string.Join(", ", claimsToAdd), string.Join(", ", claimsToRemove));

            existingScope.UserClaims.Clear();
            existingScope.UserClaims.AddRange(desiredClaims.Select(claim => new ApiScopeClaim { Type = claim }));
            
            return true;
        }
    }
}

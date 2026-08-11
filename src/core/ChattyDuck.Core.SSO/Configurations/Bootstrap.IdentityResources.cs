using ChattyDuck.Core.SSO.Data;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;

namespace ChattyDuck.Core.SSO.Configurations;

public static partial class Bootstrap
{
    public static class IdentityResources
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

                var existingResource = context.IdentityResources.SingleOrDefault(resource => resource.Name == resourceConfiguration.Name);

                if (existingResource != null)
                {
                    logger.LogInformation("Identity resource {Name} already exists.", resourceConfiguration.Name);
                    continue;
                }

                logger.LogInformation("Creating identity resource {Name}.", resourceConfiguration.Name);

                var resource = new Duende.IdentityServer.EntityFramework.Entities.IdentityResource
                {
                    Enabled = resourceConfiguration.Enabled,
                    Name = resourceConfiguration.Name,
                    DisplayName = resourceConfiguration.DisplayName,
                    Description = resourceConfiguration.Description,
                    Required = resourceConfiguration.Required,
                    Emphasize = resourceConfiguration.Emphasize,
                };

                context.IdentityResources.Add(resource);
                context.SaveChanges();

                if (resourceConfiguration.UserClaims.Any())
                {
                    var claims = resourceConfiguration.UserClaims
                        .Where(claim => !string.IsNullOrWhiteSpace(claim))
                        .Select(claim => new IdentityResourceClaim
                        {
                            IdentityResourceId = resource.Id,
                            Type = claim!
                        })
                        .ToArray();

                    context.IdentityResourceClaims.AddRange(claims);
                    context.SaveChanges();
                }
            }
        }
    }
}
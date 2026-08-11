using ChattyDuck.Core.SSO.Data;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;

namespace ChattyDuck.Core.SSO.Configurations;

public static partial class Bootstrap
{
    public static class ApiResources
    {
        public static void Configure(IServiceProvider serviceProvider, Configuration configuration)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<ApiResources>>();
            using var context = serviceProvider.GetRequiredService<ConfigurationDbContext>();

            foreach (var resourceConfiguration in configuration.ApiResources)
            {
                if (string.IsNullOrWhiteSpace(resourceConfiguration.Name))
                {
                    logger.LogWarning("Skipping API resource because its name is empty.");
                    continue;
                }

                var existingResource = context.ApiResources.SingleOrDefault(resource => resource.Name == resourceConfiguration.Name);

                if (existingResource != null)
                {
                    logger.LogInformation("API resource {Name} already exists.", resourceConfiguration.Name);
                    continue;
                }

                logger.LogInformation("Creating API resource {Name}.", resourceConfiguration.Name);

                var resource = new Duende.IdentityServer.EntityFramework.Entities.ApiResource
                {
                    Enabled = resourceConfiguration.Enabled,
                    Name = resourceConfiguration.Name,
                    DisplayName = resourceConfiguration.DisplayName,
                    Description = resourceConfiguration.Description,
                };

                context.ApiResources.Add(resource);
                context.SaveChanges();

                var claims = resourceConfiguration.UserClaims
                    .Where(claim => !string.IsNullOrWhiteSpace(claim))
                    .Select(claim => new ApiResourceClaim
                    {
                        ApiResourceId = resource.Id,
                        Type = claim!
                    })
                    .ToArray();

                if (claims.Length > 0)
                {
                    context.ApiResourceClaims.AddRange(claims);
                    context.SaveChanges();
                }

                var scopes = resourceConfiguration.Scopes
                    .Where(scope => !string.IsNullOrWhiteSpace(scope))
                    .Select(scope => new ApiResourceScope
                    {
                        ApiResourceId = resource.Id,
                        Scope = scope!
                    })
                    .ToArray();

                if (scopes.Length > 0)
                {
                    context.ApiResourceScopes.AddRange(scopes);
                    context.SaveChanges();
                }
            }
        }
    }
}

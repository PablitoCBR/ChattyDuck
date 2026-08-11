using ChattyDuck.Core.SSO.Data;
using Microsoft.EntityFrameworkCore;

namespace ChattyDuck.Core.SSO.Configurations;

public partial class Bootstrap
{
    public static void Setup(WebApplication app)
    {
        using (var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
        {
            RunDatabaseMigrations(scope.ServiceProvider);

            var configuration = app.Configuration.GetRequiredSection(Configuration.SectionName).Get<Configuration>()!;

            Users.Configure(scope.ServiceProvider, configuration);
            Bootstrap.IdentityResources.Configure(scope.ServiceProvider, configuration);
            Bootstrap.ApiResources.Configure(scope.ServiceProvider, configuration);
        }
    }

    private static void RunDatabaseMigrations(IServiceProvider serviceProvider)
    {
        using var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
    }

    private static void EnsureApiScopesConfigured(IServiceProvider serviceProvider)
    {
        using var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

        if (!context.ApiScopes.Any())
        {
            foreach (var scope in Config.ApiScopes)
            {
                context.ApiScopes.Add(scope.ToEntity());
            }
            context.SaveChanges();
        }
    }

    private static void EnsureClientsConfigured(IServiceProvider serviceProvider)
    {
        using var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

        if (!context.Clients.Any())
        {
            foreach (var client in Config.Clients)
            {
                context.Clients.Add(client.ToEntity());
            }
            context.SaveChanges();
        }
    }
}
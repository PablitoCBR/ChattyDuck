using ChattyDuck.Core.SSO.Data;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace ChattyDuck.Core.SSO.Configurations;

public partial class Bootstrap
{
    // TODO: Decide where migrations and seeding run (review point 9). Today they run on every startup in every
    // environment: several instances starting together race each other, and the seed (including the admin password)
    // would be applied in production too. Options: only with a "/seed" argument (Duende template), or a separate
    // migration job (Kubernetes Job, Container Apps job).
    public static void Setup(WebApplication app)
    {
        using var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();

        RunDatabaseMigrations(scope.ServiceProvider);

        var configuration = app.Configuration.GetRequiredSection(Configuration.SectionName).Get<Configuration>()!;

        // TODO: Seeding only adds and updates (review point 15). Clients, scopes and resources removed from configuration
        // stay in the database and stay enabled. Also: API scopes are always created with Enabled = true (no config
        // option), and identity resource claims are lowercased when compared but not when created.
        Users.Configure(scope.ServiceProvider, configuration);
        Scopes.Configure(scope.ServiceProvider, configuration);
        Clients.Configure(scope.ServiceProvider, configuration);
        ApiResources.Configure(scope.ServiceProvider, configuration);
        IdentityResources.Configure(scope.ServiceProvider, configuration);
    }

    private static void RunDatabaseMigrations(IServiceProvider serviceProvider)
    {
        serviceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
        serviceProvider.GetRequiredService<ConfigurationDbContext>().Database.Migrate();
        serviceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();
    }
}

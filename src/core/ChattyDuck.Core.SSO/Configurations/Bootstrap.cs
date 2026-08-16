using ChattyDuck.Core.SSO.Data;
using Microsoft.EntityFrameworkCore;

namespace ChattyDuck.Core.SSO.Configurations;

public partial class Bootstrap
{
    public static void Setup(WebApplication app)
    {
        using var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        
        RunDatabaseMigrations(scope.ServiceProvider);

        var configuration = app.Configuration.GetRequiredSection(Configuration.SectionName).Get<Configuration>()!;

        Users.Configure(scope.ServiceProvider, configuration);
        Scopes.Configure(scope.ServiceProvider, configuration);
        Clients.Configure(scope.ServiceProvider, configuration);
        ApiResources.Configure(scope.ServiceProvider, configuration);
        IdentityResources.Configure(scope.ServiceProvider, configuration);
    }

    private static void RunDatabaseMigrations(IServiceProvider serviceProvider)
    {
        using var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
    }
}
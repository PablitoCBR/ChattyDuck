using System.Security.Claims;
using ChattyDuck.Core.SSO.Models;
using Duende.IdentityModel;
using Microsoft.AspNetCore.Identity;

namespace ChattyDuck.Core.SSO.Configurations;

public static partial class Bootstrap
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1873:Avoid potentially expensive logging", Justification = "Startup class is not performance critical.")]
    public class Users
    {
        public static void Configure(IServiceProvider serviceProvider, Configuration configuration)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Users>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            foreach (var admin in configuration.Administrators)
            {
                var existingUser = userManager.FindByNameAsync(admin.UserName).GetAwaiter().GetResult();

                if (existingUser != null)
                {
                    logger.LogInformation("Administrator user {UserName} already exists.", admin.UserName);
                    continue;
                }

                logger.LogInformation("Creating administrator user {UserName}.", admin.UserName);

                var newUser = new ApplicationUser
                {
                    UserName = admin.UserName,
                    Email = admin.Email,
                    EmailConfirmed = false
                };

                ArgumentNullException.ThrowIfNull(admin.Password, nameof(admin.Password));
                var userCreationResult = userManager.CreateAsync(newUser, admin.Password).GetAwaiter().GetResult();

                if (userCreationResult.Succeeded)
                {
                    logger.LogInformation("Administrator user {UserName} created successfully.", admin.UserName);
                }
                else
                {
                    logger.LogCritical("Failed to create administrator user {UserName}. Errors: {Errors}", admin.UserName, string.Join(", ", userCreationResult.Errors.Select(e => e.Description)));   
                }

                var claimSetupResult = userManager.AddClaimsAsync(newUser, [
                    new Claim(JwtClaimTypes.Name, $"{admin.FirstName} {admin.LastName}"),
                    new Claim(JwtClaimTypes.GivenName, admin.FirstName),
                    new Claim(JwtClaimTypes.FamilyName, admin.LastName),
                ]).GetAwaiter().GetResult();

                if (claimSetupResult.Succeeded)
                {
                    logger.LogInformation("Claims for administrator user {UserName} set up successfully.", admin.UserName);
                }
                else
                {
                    logger.LogCritical("Failed to set up claims for administrator user {UserName}. Errors: {Errors}", admin.UserName, string.Join(", ", claimSetupResult.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}
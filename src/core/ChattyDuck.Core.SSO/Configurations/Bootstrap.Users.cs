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
        private const string AdministratorRole = "Administrator";

        public static void Configure(IServiceProvider serviceProvider, Configuration configuration)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Users>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            if (configuration.Administrators.Count == 0)
            {
                return;
            }

            if (!EnsureRoleExists(roleManager, AdministratorRole, logger))
            {
                return;
            }

            foreach (var admin in configuration.Administrators)
            {
                var user = userManager.FindByNameAsync(admin.UserName).GetAwaiter().GetResult();

                if (user is null)
                {
                    user = CreateUser(userManager, admin, logger);

                    if (user is null)
                    {
                        continue;
                    }
                }
                else
                {
                    logger.LogInformation("Administrator user {UserName} already exists.", admin.UserName);
                }

                EnsureUserInRole(userManager, user, AdministratorRole, logger);
            }
        }

        private static bool EnsureRoleExists(RoleManager<IdentityRole> roleManager, string roleName, ILogger logger)
        {
            if (roleManager.RoleExistsAsync(roleName).GetAwaiter().GetResult())
            {
                return true;
            }

            logger.LogInformation("Creating role {RoleName}.", roleName);
            var result = roleManager.CreateAsync(new IdentityRole(roleName)).GetAwaiter().GetResult();

            if (!result.Succeeded)
            {
                logger.LogCritical("Failed to create role {RoleName}. Errors: {Errors}", roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return result.Succeeded;
        }

        private static ApplicationUser? CreateUser(UserManager<ApplicationUser> userManager, ConfigurationUser admin, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(admin.Password))
            {
                logger.LogError("Skipping administrator user {UserName} because no password is configured.", admin.UserName);
                return null;
            }

            logger.LogInformation("Creating administrator user {UserName}.", admin.UserName);

            var newUser = new ApplicationUser
            {
                UserName = admin.UserName,
                Email = admin.Email,
                EmailConfirmed = false
            };

            var userCreationResult = userManager.CreateAsync(newUser, admin.Password).GetAwaiter().GetResult();

            if (!userCreationResult.Succeeded)
            {
                logger.LogCritical("Failed to create administrator user {UserName}. Errors: {Errors}", admin.UserName, string.Join(", ", userCreationResult.Errors.Select(e => e.Description)));
                return null;
            }

            logger.LogInformation("Administrator user {UserName} created successfully.", admin.UserName);

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

            return newUser;
        }

        private static void EnsureUserInRole(UserManager<ApplicationUser> userManager, ApplicationUser user, string roleName, ILogger logger)
        {
            if (userManager.IsInRoleAsync(user, roleName).GetAwaiter().GetResult())
            {
                return;
            }

            var result = userManager.AddToRoleAsync(user, roleName).GetAwaiter().GetResult();

            if (result.Succeeded)
            {
                logger.LogInformation("User {UserName} added to role {RoleName}.", user.UserName, roleName);
            }
            else
            {
                logger.LogCritical("Failed to add user {UserName} to role {RoleName}. Errors: {Errors}", user.UserName, roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}

using ChattyDuck.Core.SSO.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChattyDuck.Core.SSO.Data;

/// <summary>
/// ASP.NET Identity tables plus the ASP.NET Core Data Protection key ring.
/// </summary>
/// <remarks>
/// The Data Protection keys live in the same database as the users because they have the same lifetime and the same
/// backup needs. Losing them makes every IdentityServer signing key (which is encrypted with Data Protection)
/// and every login cookie unreadable.
/// </remarks>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
}

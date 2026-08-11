using System.ComponentModel.DataAnnotations;
using System.Security;

namespace ChattyDuck.Core.SSO.Configurations;

public class Configuration
{
    public const string SectionName = "IdentityServer";

    [Required]
    public IReadOnlyCollection<ConfigurationUser> Administrators { get; set; } = Array.Empty<ConfigurationUser>();

    [Required]
    public IReadOnlyCollection<ConfigurationIdentityResource> IdentityResources { get; set; } = Array.Empty<ConfigurationIdentityResource>();

    [Required]
    public IReadOnlyCollection<ConfigurationApiResource> ApiResources { get; set; } = Array.Empty<ConfigurationApiResource>();
}

public class ConfigurationUser
{
    [Required, MinLength(1), MaxLength(100)]
    public string UserName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(1), MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MinLength(1), MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, MinLength(1), MaxLength(100)]
    public SecureString Password { get; set; } = default!;
}

public class ConfigurationIdentityResource
{
    [Required, MinLength(1), MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MinLength(1), MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool Enabled { get; set; } = true;

    public bool Required { get; set; }

    public bool Emphasize { get; set; }

    public IReadOnlyCollection<string> UserClaims { get; set; } = Array.Empty<string>();
}

public class ConfigurationApiResource
{
    [Required, MinLength(1), MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MinLength(1), MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool Enabled { get; set; } = true;

    public IReadOnlyCollection<string> Scopes { get; set; } = Array.Empty<string>();

    public IReadOnlyCollection<string> UserClaims { get; set; } = Array.Empty<string>();
}
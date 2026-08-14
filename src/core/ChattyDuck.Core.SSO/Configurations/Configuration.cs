using System.ComponentModel.DataAnnotations;
using System.Security;

namespace ChattyDuck.Core.SSO.Configurations;

public class Configuration
{
    public const string SectionName = "IdentityServer";

    [Required]
    public IReadOnlyCollection<ConfigurationScope> Scopes { get; set; } = Array.Empty<ConfigurationScope>();

    [Required]
    public IReadOnlyCollection<ConfigurationClient> Clients { get; set; } = Array.Empty<ConfigurationClient>();

    [Required]
    public IReadOnlyCollection<ConfigurationUser> Administrators { get; set; } = Array.Empty<ConfigurationUser>();

    [Required]
    public IReadOnlyCollection<ConfigurationApiResource> ApiResources { get; set; } = Array.Empty<ConfigurationApiResource>();

    [Required]
    public IReadOnlyCollection<ConfigurationIdentityResource> IdentityResources { get; set; } = Array.Empty<ConfigurationIdentityResource>();
}

public class ConfigurationClient
{
    [Required, MinLength(1), MaxLength(100)]
    public required string ClientId { get; set; }

    public bool Enabled { get; set; } = true;
    
    [Required, MinLength(1), MaxLength(100)]
    public required string ClientName { get; set; }

    public string Description { get; internal set; } = string.Empty;
    public required IEnumerable<string> AllowedGrantTypes { get; set; }
    public required IEnumerable<string> RedirectUris { get; set; }
    public required IEnumerable<string> PostLogoutRedirectUris { get; set; }
    public required IEnumerable<string> AllowedScopes { get; set; }
}

public class ConfigurationScope
{
    [Required, MinLength(1), MaxLength(100)]
    public required string Name { get; set; }

    [Required, MinLength(1), MaxLength(256)]
    public required string DisplayName { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Defines if the scope is mandatory to use app if true, otherwise it is opt-in. If the scope is required, the user will not be able to uncheck it during consent.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Defines if the scope should be emphasized during consent screen. If true, the scope will be highlighted and shown separately from other scopes.
    /// Scope should be emphasized if it is a sensitive scope, such as a scope that grants access to personal information or a scope that allows the application to perform actions on behalf of the user.
    /// </summary>
    public bool Emphasize { get; set; }

    public IReadOnlyCollection<string> UserClaims { get; set; } = Array.Empty<string>();
}

public class ConfigurationUser
{
    [Required, MinLength(1), MaxLength(100)]
    public required string UserName { get; set; }

    [Required, EmailAddress]
    public required string Email { get; set; }

    [Required, MinLength(1), MaxLength(100)]
    public required string FirstName { get; set; }

    [Required, MinLength(1), MaxLength(100)]
    public required string LastName { get; set; }

    [Required, MinLength(1), MaxLength(100)]
    public required SecureString Password { get; set; }
}

public class ConfigurationIdentityResource
{
    [Required, MinLength(1), MaxLength(200)]
    public required string Name { get; set; }

    [Required, MinLength(1), MaxLength(200)]
    public required string DisplayName { get; set; }

    public string? Description { get; set; }

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Defines if the identity resource is mandatory to use app if true, otherwise it is opt-in. If the identity resource is required, the user will not be able to uncheck it during consent.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Defines if the identity resource should be emphasized during consent screen. If true, the identity resource will be highlighted and shown separately from other identity resources.
    /// Identity resource should be emphasized if it is a sensitive identity resource, such as an identity resource that grants access to personal information or an identity resource that allows the application to perform actions on behalf of the user.
    /// </summary>
    public bool Emphasize { get; set; }

    public IReadOnlyCollection<string> UserClaims { get; set; } = Array.Empty<string>();
}

public class ConfigurationApiResource
{
    [Required, MinLength(1), MaxLength(200)]
    public required string Name { get; set; }

    [Required, MinLength(1), MaxLength(200)]
    public required string DisplayName { get; set; }

    public string? Description { get; set; }

    public bool Enabled { get; set; } = true;

    public IReadOnlyCollection<string> Scopes { get; set; } = Array.Empty<string>();

    public IReadOnlyCollection<string> UserClaims { get; set; } = Array.Empty<string>();
}
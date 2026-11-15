using System.ComponentModel.DataAnnotations;
using Duende.IdentityServer.Models;

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

    public string Description { get; set; } = string.Empty;
    public required IEnumerable<string> AllowedGrantTypes { get; set; }
    public required IEnumerable<string> RedirectUris { get; set; }
    public required IEnumerable<string> PostLogoutRedirectUris { get; set; }
    public required IEnumerable<string> AllowedScopes { get; set; }

    /// <summary>
    /// Origins (scheme + host + port, no path) allowed to call IdentityServer endpoints from browser JavaScript.
    /// Only needed when a browser talks to IdentityServer directly; a BFF / server-side client does not need it.
    /// </summary>
    public IEnumerable<string> AllowedCorsOrigins { get; set; } = [];

    /// <summary>
    /// Confidential clients (a server that can keep a secret, e.g. a BFF) must authenticate at the token endpoint.
    /// Public clients (native apps, browser-only SPAs) cannot keep a secret and set this to false.
    /// The secure default mirrors Duende's default: a secret is required.
    /// </summary>
    public bool RequireClientSecret { get; set; } = true;

    /// <summary>
    /// Shared secrets of the client. Values are plain text in configuration and stored as SHA-256 hashes.
    /// Never put real values in appsettings: supply them via user-secrets, environment variables or a secret store.
    /// </summary>
    public IEnumerable<ConfigurationClientSecret> ClientSecrets { get; set; } = [];

    /// <summary>
    /// Proof Key for Code Exchange. Recommended for every authorization_code client, confidential ones included (OAuth 2.1 / BCP).
    /// </summary>
    public bool RequirePkce { get; set; } = true;

    /// <summary>
    /// Allows requesting the offline_access scope, which issues refresh tokens.
    /// </summary>
    public bool AllowOfflineAccess { get; set; }

    /// <summary>Access token lifetime in seconds (Duende default: 3600 = 1 hour).</summary>
    [Range(1, int.MaxValue)]
    public int AccessTokenLifetime { get; set; } = 3600;

    /// <summary>Identity token lifetime in seconds (Duende default: 300 = 5 minutes).</summary>
    [Range(1, int.MaxValue)]
    public int IdentityTokenLifetime { get; set; } = 300;

    /// <summary>Maximum refresh token lifetime in seconds (Duende default: 2592000 = 30 days).</summary>
    [Range(1, int.MaxValue)]
    public int AbsoluteRefreshTokenLifetime { get; set; } = 2592000;

    /// <summary>Sliding refresh token lifetime in seconds, only used with <see cref="TokenExpiration.Sliding"/> (Duende default: 1296000 = 15 days).</summary>
    [Range(0, int.MaxValue)]
    public int SlidingRefreshTokenLifetime { get; set; } = 1296000;

    /// <summary>
    /// OneTimeOnly rotates the refresh token on every use, so a stolen token is detected when it is replayed. Recommended.
    /// </summary>
    public TokenUsage RefreshTokenUsage { get; set; } = TokenUsage.OneTimeOnly;

    /// <summary>
    /// Absolute: the refresh token expires at a fixed time. Sliding: every use extends it, capped by the absolute lifetime.
    /// </summary>
    public TokenExpiration RefreshTokenExpiration { get; set; } = TokenExpiration.Absolute;
}

public class ConfigurationClientSecret
{
    [Required, MinLength(16)]
    public required string Value { get; set; }

    public string? Description { get; set; }

    /// <summary>Optional expiration (UTC). Lets a secret be rotated by adding a new one before the old one expires.</summary>
    public DateTime? Expiration { get; set; }
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
    public required string Password { get; set; }
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
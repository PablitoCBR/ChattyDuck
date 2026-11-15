namespace ChattyDuck.Core.SSO.Configurations;

/// <summary>
/// Settings for running behind a reverse proxy (Envoy in compose and Kubernetes, the platform ingress in the cloud).
/// </summary>
/// <remarks>
/// <para>
/// A reverse proxy terminates TLS and opens a new plain-HTTP connection to the SSO. Without forwarded headers the SSO
/// would think every request came over <c>http://</c> from the proxy's IP address. IdentityServer derives its issuer
/// and every endpoint URL in the discovery document from the incoming request, so it would advertise <c>http://</c>
/// URLs, and token validation in clients (which compare the issuer exactly) would fail.
/// </para>
/// <para>
/// The proxy reports the original scheme and client IP in <c>X-Forwarded-Proto</c> and <c>X-Forwarded-For</c>.
/// Those headers are trivial to forge, so they are only honoured when the request comes from a proxy listed in
/// <see cref="KnownProxies"/> or <see cref="KnownNetworks"/> (loopback is always trusted by ASP.NET Core).
/// </para>
/// <para>
/// <c>X-Forwarded-Host</c> is deliberately not processed: Envoy and the common ingresses preserve the original
/// <c>Host</c> header, so trusting another header for it would only add a host-injection risk.
/// </para>
/// </remarks>
public sealed class ForwardedHeadersSettings
{
    public const string SectionName = "ForwardedHeaders";

    /// <summary>Turns forwarded-headers processing on. Off when the SSO is reached directly (for example <c>dotnet run</c>).</summary>
    public bool Enabled { get; set; }

    /// <summary>Exact IP addresses of trusted proxies, for example <c>10.0.0.5</c>.</summary>
    public IReadOnlyCollection<string> KnownProxies { get; set; } = [];

    /// <summary>Trusted proxy networks in CIDR notation, for example <c>172.16.0.0/12</c> for Docker networks.</summary>
    public IReadOnlyCollection<string> KnownNetworks { get; set; } = [];

    /// <summary>
    /// How many proxy hops to unwind from <c>X-Forwarded-For</c>. 1 means "one proxy in front of us", which is the T3 topology.
    /// </summary>
    public int ForwardLimit { get; set; } = 1;
}

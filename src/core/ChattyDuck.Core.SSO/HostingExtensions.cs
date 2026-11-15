using System.Globalization;
using System.Net;
using Duende.IdentityServer;
using ChattyDuck.Core.SSO.Configurations;
using ChattyDuck.Core.SSO.Data;
using ChattyDuck.Core.SSO.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Filters;

namespace ChattyDuck.Core.SSO;

internal static class HostingExtensions
{
    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        // Write most logs to the console but diagnostic data to a file.
        // See https://docs.duendesoftware.com/identityserver/diagnostics/data
        builder.Services.AddSerilog(lc =>
        {
            // Minimum levels and overrides come from the "Serilog" configuration section in every environment, so
            // verbosity can be changed per environment (appsettings.{Environment}.json, environment variables)
            // without a code change. Previously this was only applied inside the Development branch below, so other
            // environments silently ignored the section.
            lc.ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext();

            lc.WriteTo.Logger(consoleLogger =>
            {
                consoleLogger.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
                    formatProvider: CultureInfo.InvariantCulture);
                if (builder.Environment.IsDevelopment())
                {
                    consoleLogger.Filter.ByExcluding(Matching.FromSource("Duende.IdentityServer.Diagnostics.Summary"));
                }
            });
            if (builder.Environment.IsDevelopment())
            {
                lc.WriteTo.Logger(fileLogger =>
                {
                    fileLogger
                        .WriteTo.File("./diagnostics/diagnostic.log", rollingInterval: RollingInterval.Day,
                            fileSizeLimitBytes: 1024 * 1024 * 10, // 10 MB
                            rollOnFileSizeLimit: true,
                            outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
                            formatProvider: CultureInfo.InvariantCulture)
                        .Filter
                        .ByIncludingOnly(Matching.FromSource("Duende.IdentityServer.Diagnostics.Summary"));
                });
            }
        });
        return builder;
    }

    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorPages();

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // ASP.NET Core Data Protection encrypts IdentityServer's automatically managed signing keys, the login cookie
        // and anti-forgery tokens. By default its key ring lives on the local file system, which is lost whenever a
        // container restarts and is not shared between instances. Persisting it in PostgreSQL keeps tokens and
        // sessions valid across restarts and lets several instances read each other's keys.
        // A fixed application name is required so that every instance (and every new deployment) uses the same key ring.
        // See https://docs.duendesoftware.com/identityserver/deployment/#aspnet-core-data-protection
        builder.Services.AddDataProtection()
            .SetApplicationName("ChattyDuck.Core.SSO")
            .PersistKeysToDbContext<ApplicationDbContext>();
        // TODO: Encrypt the Data Protection keys at rest outside local development (ProtectKeysWithCertificate,
        // or a cloud key vault behind an abstraction + ADR). Today they are stored unencrypted in the database.

        builder.Services
            .AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                // TODO: Read the Duende license key from configuration (review point 14), e.g.
                // options.LicenseKey = builder.Configuration["Duende:LicenseKey"]. Automatic key management is a
                // paid-edition feature; check whether the free Community Edition applies to this project.
                // Use a section other than "IdentityServer", which already holds the seed data.

                // Automatic key management, when opertional store is implemented with EF the keys are persisted to the database and automatically rotated.
                options.KeyManagement.Enabled = true; 

                // Cache lifetimes for the configuration store cache (AddConfigurationStoreCache below).
                // Trade-off: a longer lifetime means fewer database reads, but a change in the database (for example a
                // disabled client or a removed redirect URI) takes up to this long to take effect, separately on every
                // instance. 5 minutes keeps that window short while still removing almost all repeated reads.
                // (Duende's default is 15 minutes.)
                var configurationCacheLifetime = TimeSpan.FromMinutes(5);
                options.Caching.ClientStoreExpiration = configurationCacheLifetime;
                options.Caching.ResourceStoreExpiration = configurationCacheLifetime;
                options.Caching.CorsExpiration = configurationCacheLifetime;
                options.Caching.IdentityProviderCacheDuration = configurationCacheLifetime;

                // Use a large chunk size for diagnostic data in development where it will be redirected to a local file.
                if (builder.Environment.IsDevelopment())
                {
                    options.Diagnostics.ChunkSize = 1024 * 1024 * 10; // 10 MB
                }
            })
            .AddConfigurationStore(options =>
            {
                options.ConfigureDbContext = b => b.UseNpgsql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    sql => sql.MigrationsAssembly(typeof(HostingExtensions).Assembly.FullName));
            })
            // Without this, every authorize and token request reads the client, its scopes and resources, and the CORS
            // origins from PostgreSQL again, although they almost never change. This wraps the EF stores in an
            // in-memory cache (per instance) using the lifetimes set in options.Caching above.
            .AddConfigurationStoreCache()
            .AddOperationalStore(options =>
            {
                options.ConfigureDbContext = b => b.UseNpgsql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    sql => sql.MigrationsAssembly(typeof(HostingExtensions).Assembly.FullName));

                // The operational store keeps refresh tokens, authorization codes, consents, device codes and
                // pushed authorization requests. IdentityServer ignores expired rows, but never deletes them on its
                // own, so without cleanup the tables only grow. This background job deletes expired and consumed rows.
                // With several instances every instance runs it; the deletes are idempotent, so that is safe, and
                // Duende recommends enabling it on all instances (or on one dedicated instance for very large stores).
                options.EnableTokenCleanup = true;
                options.TokenCleanupInterval = 3600; // seconds; Duende's default, stated explicitly
                // One-time refresh tokens are marked "consumed" when they are used. Delete those as well, but keep them
                // for a day first: a consumed token being presented again is the signal of a stolen refresh token,
                // and the rows are what lets you investigate (or later, revoke the user's other tokens).
                options.RemoveConsumedTokens = true;
                options.ConsumedTokenCleanupDelay = (int)TimeSpan.FromDays(1).TotalSeconds;
            })
            .AddAspNetIdentity<ApplicationUser>()
            .AddLicenseSummary();

        builder.Services.AddAuthentication();

        ConfigureForwardedHeaders(builder);

        return builder.Build();
    }

    private static ForwardedHeadersSettings GetForwardedHeadersSettings(IConfiguration configuration) =>
        configuration.GetSection(ForwardedHeadersSettings.SectionName).Get<ForwardedHeadersSettings>() ?? new ForwardedHeadersSettings();

    /// <summary>
    /// Trust X-Forwarded-For / X-Forwarded-Proto only from configured proxies. See <see cref="ForwardedHeadersSettings"/> for the reasoning.
    /// </summary>
    private static void ConfigureForwardedHeaders(WebApplicationBuilder builder)
    {
        var settings = GetForwardedHeadersSettings(builder.Configuration);

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = settings.ForwardLimit;

            // The defaults (loopback only) stay in place; configured proxies are added to them.
            foreach (var proxy in settings.KnownProxies)
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }

            foreach (var network in settings.KnownNetworks)
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }
        });
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Must run first, so that everything after it (request logging, HTTPS redirection, IdentityServer's issuer
        // and URL generation) sees the original scheme and client IP instead of the proxy's.
        if (GetForwardedHeadersSettings(app.Configuration).Enabled)
        {
            app.UseForwardedHeaders();
        }

        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            // Never show stack traces outside development: render the generic error page instead.
            app.UseExceptionHandler("/Error");

            // HSTS tells the browser to use HTTPS only for this host for a year, so a downgrade to HTTP is impossible
            // even on the very first request of a later visit. It is skipped in Development on purpose: browsers cache
            // it per host, and an HSTS entry for "localhost" would then force HTTPS on every other local app as well.
            app.UseHsts();
        }

        // Redirect plain HTTP to HTTPS. Behind a TLS-terminating proxy the request is already HTTPS as far as the app
        // is concerned (thanks to X-Forwarded-Proto above), so this does not cause a redirect loop.
        // The public HTTPS port used in the redirect comes from configuration (https_port / ASPNETCORE_HTTPS_PORT).
        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseRouting();
        app.UseIdentityServer(); // Using Identity Server middleware implies also UseAuthentication.
        app.UseAuthorization();

        app.MapRazorPages()
            .RequireAuthorization();

        return app;
    }
}

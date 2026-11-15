namespace ChattyDuck.Core.SSO.Pages;

internal static class Log
{
    private static readonly Action<ILogger, string?, Exception?> _invalidId = LoggerMessage.Define<string?>(
        LogLevel.Error,
        EventIds.InvalidId,
        "Invalid id {Id}");

    public static void InvalidId(this ILogger logger, string? id) => _invalidId(logger, id, null);

    private static readonly Action<ILogger, IEnumerable<string>, Exception?> _externalClaims = LoggerMessage.Define<IEnumerable<string>>(
        LogLevel.Debug,
        EventIds.ExternalClaims,
        "External claims: {Claims}");

    public static void ExternalClaims(this ILogger logger, IEnumerable<string> claims) => _externalClaims(logger, claims, null);

    private static Action<ILogger, string, Exception?> _noConsentMatchingRequest = LoggerMessage.Define<string>(
        LogLevel.Error,
        EventIds.NoConsentMatchingRequest,
        "No consent request matching request: {ReturnUrl}");

    public static void NoConsentMatchingRequest(this ILogger logger, string returnUrl) => _noConsentMatchingRequest(logger, returnUrl, null);


}

internal static class EventIds
{
    private const int UIEventsStart = 10000;

    //////////////////////////////
    // Consent
    //////////////////////////////
    private const int ConsentEventsStart = UIEventsStart + 1000;
    public const int InvalidId = ConsentEventsStart + 0;
    public const int NoConsentMatchingRequest = ConsentEventsStart + 1;

    //////////////////////////////
    // External Login
    //////////////////////////////
    private const int ExternalLoginEventsStart = UIEventsStart + 2000;
    public const int ExternalClaims = ExternalLoginEventsStart + 0;



}

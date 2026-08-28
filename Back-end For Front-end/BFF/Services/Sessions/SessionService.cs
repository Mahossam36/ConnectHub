using System.Security.Cryptography;
using SessionConfiguration = BFF.Configuration.SessionOptions;
using BFF.Models.Auth;
using BFF.Models.Sessions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace BFF.Services.Sessions;

public sealed class SessionService(ISessionStore sessionStore, IOptions<SessionConfiguration> options) : ISessionService
{
    private readonly SessionConfiguration _options = options.Value;
    private TimeSpan Lifetime => TimeSpan.FromMinutes(_options.ExpirationMinutes);

    public async Task<UserSession> CreateAsync(UpstreamAuthResponse authentication, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var session = new UserSession(
            Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), authentication.UserId, authentication.Email,
            authentication.DisplayName, authentication.AvatarUrl, authentication.AccessToken, authentication.RefreshToken,
            authentication.ExpiresAt, null, now, now.Add(Lifetime));
        await sessionStore.StoreAsync(session, Lifetime, cancellationToken);
        return session;
    }

    public Task<UserSession?> GetCurrentAsync(HttpContext context, CancellationToken cancellationToken = default) =>
        context.Request.Cookies.TryGetValue(_options.CookieName, out var sessionId) && !string.IsNullOrWhiteSpace(sessionId)
            ? sessionStore.GetAsync(sessionId, cancellationToken)
            : Task.FromResult<UserSession?>(null);

    public Task<UserSession?> GetAsync(string sessionId, CancellationToken cancellationToken = default) =>
        sessionStore.GetAsync(sessionId, cancellationToken);

    public Task UpdateAsync(UserSession session, CancellationToken cancellationToken = default) =>
        sessionStore.StoreAsync(session, Lifetime, cancellationToken);

    public async Task RemoveCurrentAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        if (context.Request.Cookies.TryGetValue(_options.CookieName, out var sessionId) && !string.IsNullOrWhiteSpace(sessionId))
            await sessionStore.RemoveAsync(sessionId, cancellationToken);
        ClearCookie(context);
    }

    public void SetCookie(HttpContext context, string sessionId) => context.Response.Cookies.Append(_options.CookieName, sessionId, new CookieOptions
    {
        HttpOnly = true, Secure = ShouldSecure(context), SameSite = ParseSameSite(), IsEssential = true,
        Path = "/", MaxAge = Lifetime
    });

    public void ClearCookie(HttpContext context) => context.Response.Cookies.Delete(_options.CookieName, new CookieOptions
    {
        HttpOnly = true, Secure = ShouldSecure(context), SameSite = ParseSameSite(), IsEssential = true, Path = "/"
    });

    private bool ShouldSecure(HttpContext context) => Enum.TryParse<CookieSecurePolicy>(_options.SecurePolicy, true, out var policy)
        ? policy == CookieSecurePolicy.Always || policy == CookieSecurePolicy.SameAsRequest && context.Request.IsHttps
        : true;
    private SameSiteMode ParseSameSite() => Enum.TryParse<SameSiteMode>(_options.SameSite, true, out var mode)
        ? mode : SameSiteMode.None;
}

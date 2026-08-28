using System.Security.Claims;
using BFF.Configuration;
using BFF.Models.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace BFF.Services.Google;

public sealed class GoogleAuthenticationService(
    IOptions<GoogleOptions> options,
    ILogger<GoogleAuthenticationService> logger) : IGoogleAuthenticationService
{
    public const string OpenIdConnectScheme = "GoogleOidc";
    public const string ExternalCookieScheme = "GoogleExternal";
    private readonly GoogleOptions _options = options.Value;

    public AuthenticationProperties CreateChallengeProperties()
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret))
            throw new InvalidOperationException("Google authentication is not configured.");
        if (!Uri.TryCreate(_options.RedirectUri, UriKind.Absolute, out var redirectUri) ||
            redirectUri.Scheme != Uri.UriSchemeHttps || redirectUri.AbsolutePath != _options.CallbackPath)
            throw new InvalidOperationException("Google HTTPS redirect configuration is invalid.");

        return new AuthenticationProperties { RedirectUri = "/auth/google/complete" };
    }

    public async Task<GoogleIdentityResult> GetValidatedIdentityAsync(HttpContext context)
    {
        var result = await context.AuthenticateAsync(ExternalCookieScheme);
        if (!result.Succeeded || result.Principal is null)
        {
            logger.LogWarning("Google external authentication did not produce a validated principal.");
            return GoogleIdentityResult.AuthenticationFailed();
        }

        var subject = result.Principal.FindFirstValue("sub");
        var email = result.Principal.FindFirstValue("email");
        var firstName = result.Principal.FindFirstValue("given_name");
        var lastName = result.Principal.FindFirstValue("family_name");
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            logger.LogWarning("Google authenticated identity is missing required profile claims.");
            return GoogleIdentityResult.IncompleteProfile();
        }

        return GoogleIdentityResult.Success(new GoogleIdentity(
            subject,
            email,
            firstName,
            lastName,
            result.Principal.FindFirstValue("picture")));
    }

    public Task ClearExternalAuthenticationAsync(HttpContext context) =>
        context.SignOutAsync(ExternalCookieScheme);
}

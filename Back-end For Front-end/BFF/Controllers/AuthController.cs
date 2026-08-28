using BFF.Models.Auth;
using BFF.Configuration;
using BFF.Services.Authentication;
using BFF.Services.Google;
using BFF.Services.Sessions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BFF.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController(
    IAuthenticationClient authenticationClient,
    ISessionService sessionService,
    IGoogleAuthenticationService googleAuthenticationService,
    IOptions<GoogleOptions> googleOptions) : ControllerBase
{
    [HttpPost("login")]
    public Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken) =>
        EstablishSessionAsync(authenticationClient.LoginAsync(request, cancellationToken), cancellationToken);

    [HttpPost("register")]
    public Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken) =>
        EstablishSessionAsync(authenticationClient.RegisterAsync(request, cancellationToken), cancellationToken);

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var session = await sessionService.GetCurrentAsync(HttpContext, cancellationToken);
        if (session is not null)
        {
            var result = await authenticationClient.LogoutAsync(session.RefreshToken, cancellationToken);
            if (!result.Succeeded && result.StatusCode is not (StatusCodes.Status401Unauthorized or StatusCodes.Status404NotFound))
                return new ContentResult { StatusCode = result.StatusCode, Content = result.ErrorBody, ContentType = "application/json" };
        }
        await sessionService.RemoveCurrentAsync(HttpContext, cancellationToken);
        return NoContent();
    }

    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Revoke(CancellationToken cancellationToken)
    {
        var session = await sessionService.GetCurrentAsync(HttpContext, cancellationToken);
        if (session is null)
        {
            sessionService.ClearCookie(HttpContext);
            return Unauthorized();
        }

        var result = await authenticationClient.RevokeAsync(session.RefreshToken, cancellationToken);
        if (!result.Succeeded)
            return new ContentResult { StatusCode = result.StatusCode, Content = result.ErrorBody, ContentType = "application/json" };

        await sessionService.RemoveCurrentAsync(HttpContext, cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(AuthenticatedUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var session = await sessionService.GetCurrentAsync(HttpContext, cancellationToken);
        if (session is null)
        {
            sessionService.ClearCookie(HttpContext);
            return Unauthorized();
        }

        return Ok(ToSafeResponse(session));
    }

    [HttpGet("google")]
    public IActionResult Google() =>
        Challenge(googleAuthenticationService.CreateChallengeProperties(), GoogleAuthenticationService.OpenIdConnectScheme);

    [HttpGet("google/complete")]
    public async Task<IActionResult> GoogleComplete(CancellationToken cancellationToken)
    {
        try
        {
            var identityResult = await googleAuthenticationService.GetValidatedIdentityAsync(HttpContext);
            if (identityResult.Identity is null)
            {
                return identityResult.Failure == GoogleIdentityFailure.IncompleteProfile
                    ? BadRequest(new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Google profile is incomplete.",
                        Detail = "Google did not provide the name and email information required to create a Yalla account."
                    })
                    : Unauthorized();
            }

            var identity = identityResult.Identity;

            var request = new ExternalLoginRequest(
                Provider: "Google",
                ProviderId: identity.Subject,
                Email: identity.Email,
                FirstName: identity.FirstName,
                LastName: identity.LastName,
                ProfileImageUrl: identity.AvatarUrl);
            var result = await authenticationClient.ExternalLoginAsync(request, cancellationToken);
            if (!result.Succeeded || result.Authentication is null)
                return new ContentResult { StatusCode = result.StatusCode, Content = result.ErrorBody, ContentType = "application/json" };

            var session = await sessionService.CreateAsync(result.Authentication, cancellationToken);
            sessionService.SetCookie(HttpContext, session.SessionId);
            var redirectUri = googleOptions.Value.FrontendRedirectUri;
            if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var frontendUri) ||
                frontendUri.Scheme is not ("https" or "http"))
                throw new InvalidOperationException("Google frontend redirect is not configured.");
            return Redirect(frontendUri.ToString());
        }
        finally
        {
            await googleAuthenticationService.ClearExternalAuthenticationAsync(HttpContext);
        }
    }

    private async Task<IActionResult> EstablishSessionAsync(Task<AuthenticationCallResult> authenticationTask, CancellationToken cancellationToken)
    {
        var result = await authenticationTask;
        if (!result.Succeeded || result.Authentication is null)
            return new ContentResult { StatusCode = result.StatusCode, Content = result.ErrorBody, ContentType = "application/json" };

        var session = await sessionService.CreateAsync(result.Authentication, cancellationToken);
        sessionService.SetCookie(HttpContext, session.SessionId);
        return StatusCode(result.StatusCode, ToSafeResponse(session));
    }

    private static AuthenticatedUserResponse ToSafeResponse(Models.Sessions.UserSession session) =>
        new(session.UserId, session.Email, session.DisplayName, session.AvatarUrl, session.ExpiresAt);
}

using BFF.Models.Auth;
using Microsoft.AspNetCore.Authentication;

namespace BFF.Services.Google;

public interface IGoogleAuthenticationService
{
    AuthenticationProperties CreateChallengeProperties();
    Task<GoogleIdentityResult> GetValidatedIdentityAsync(HttpContext context);
    Task ClearExternalAuthenticationAsync(HttpContext context);
}

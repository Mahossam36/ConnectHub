using BFF.Configuration;
using BFF.Health;
using BFF.Middleware;
using BFF.Services.Authentication;
using BFF.Services.Google;
using BFF.Services.Integration;
using BFF.Services.Moderation;
using BFF.Services.Sessions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using SessionConfiguration = BFF.Configuration.SessionOptions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<IntegrationOptions>(builder.Configuration.GetSection(IntegrationOptions.SectionName));
builder.Services.Configure<AuthenticationOptions>(builder.Configuration.GetSection(AuthenticationOptions.SectionName));
builder.Services.Configure<SessionConfiguration>(builder.Configuration.GetSection(SessionConfiguration.SectionName));
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection(RedisOptions.SectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));
builder.Services.Configure<GoogleOptions>(builder.Configuration.GetSection(GoogleOptions.SectionName));
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection(OpenAiOptions.SectionName));

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);
builder.Services.AddHttpClient("IntegrationLayer", client => client.Timeout = TimeSpan.FromSeconds(100));
var openAiConfiguration = builder.Configuration.GetSection(OpenAiOptions.SectionName).Get<OpenAiOptions>() ?? new OpenAiOptions();
builder.Services.AddHttpClient("OpenAI", client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/v1/");
    client.Timeout = TimeSpan.FromSeconds(openAiConfiguration.Moderation.TimeoutSeconds);
});
var googleConfiguration = builder.Configuration.GetSection(GoogleOptions.SectionName).Get<GoogleOptions>() ?? new GoogleOptions();
if (!builder.Environment.IsDevelopment())
{
    var integrationConfiguration = builder.Configuration.GetSection(IntegrationOptions.SectionName).Get<IntegrationOptions>() ?? new IntegrationOptions();
    var authenticationConfiguration = builder.Configuration.GetSection(AuthenticationOptions.SectionName).Get<AuthenticationOptions>() ?? new AuthenticationOptions();
    var missing = new[]
    {
        (integrationConfiguration.BaseUrl, "Integration:BaseUrl"),
        (integrationConfiguration.ConnectHubPath, "Integration:ConnectHubPath"),
        (integrationConfiguration.TokenPath, "Integration:TokenPath"),
        (integrationConfiguration.TokenRevokePath, "Integration:TokenRevokePath"),
        (integrationConfiguration.ClientId, "Integration:ClientId"),
        (integrationConfiguration.ClientSecret, "Integration:ClientSecret"),
        (authenticationConfiguration.LoginPath, "Authentication:LoginPath"),
        (authenticationConfiguration.RegisterPath, "Authentication:RegisterPath"),
        (authenticationConfiguration.ExternalLoginPath, "Authentication:ExternalLoginPath"),
        (authenticationConfiguration.RefreshPath, "Authentication:RefreshPath")
    }.Where(item => string.IsNullOrWhiteSpace(item.Item1)).Select(item => item.Item2).ToArray();
    if (missing.Length > 0) throw new InvalidOperationException($"Required BFF configuration is missing: {string.Join(", ", missing)}.");
}
builder.Services.AddAuthentication()
    .AddCookie(GoogleAuthenticationService.ExternalCookieScheme, cookie =>
    {
        cookie.Cookie.Name = "Yalla.Google.External";
        cookie.Cookie.HttpOnly = true;
        cookie.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        cookie.Cookie.SameSite = SameSiteMode.Lax;
        cookie.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        cookie.SlidingExpiration = false;
    })
    .AddOpenIdConnect(GoogleAuthenticationService.OpenIdConnectScheme, oidc =>
    {
        oidc.Authority = "https://accounts.google.com";
        oidc.ClientId = googleConfiguration.ClientId;
        oidc.ClientSecret = googleConfiguration.ClientSecret;
        oidc.CallbackPath = googleConfiguration.CallbackPath;
        oidc.SignInScheme = GoogleAuthenticationService.ExternalCookieScheme;
        oidc.ResponseType = "code";
        oidc.UsePkce = true;
        oidc.RequireHttpsMetadata = true;
        oidc.SaveTokens = false;
        oidc.GetClaimsFromUserInfoEndpoint = true;
        oidc.MapInboundClaims = false;
        oidc.Scope.Clear();
        oidc.Scope.Add("openid");
        oidc.Scope.Add("email");
        oidc.Scope.Add("profile");
        oidc.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            NameClaimType = "name"
        };
        oidc.Events.OnRemoteFailure = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return context.Response.CompleteAsync();
        };
        oidc.Events.OnRedirectToIdentityProvider = context =>
        {
            if (!string.IsNullOrWhiteSpace(googleConfiguration.RedirectUri))
                context.ProtocolMessage.RedirectUri = googleConfiguration.RedirectUri;
            return Task.CompletedTask;
        };
        oidc.Events.OnAuthorizationCodeReceived = context =>
        {
            if (!string.IsNullOrWhiteSpace(googleConfiguration.RedirectUri) && context.TokenEndpointRequest is not null)
                context.TokenEndpointRequest.RedirectUri = googleConfiguration.RedirectUri;
            return Task.CompletedTask;
        };
        oidc.Events.OnUserInformationReceived = context =>
        {
            if (context.Principal?.Identity is not ClaimsIdentity identity) return Task.CompletedTask;
            foreach (var claimName in new[] { "sub", "email", "given_name", "family_name", "picture" })
            {
                if (identity.HasClaim(claim => claim.Type == claimName) ||
                    !context.User.RootElement.TryGetProperty(claimName, out var value) || value.ValueKind != System.Text.Json.JsonValueKind.String)
                    continue;
                identity.AddClaim(new Claim(claimName, value.GetString()!));
            }
            return Task.CompletedTask;
        };
    });
builder.Services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
{
    var redis = serviceProvider.GetRequiredService<IOptions<RedisOptions>>().Value;
    if (string.IsNullOrWhiteSpace(redis.ConnectionString)) throw new InvalidOperationException("Redis connection configuration is missing.");
    var configuration = ConfigurationOptions.Parse(redis.ConnectionString);
    configuration.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(configuration);
});

builder.Services.AddSingleton<ISessionStore, RedisSessionStore>();
builder.Services.AddSingleton<ISessionService, SessionService>();
builder.Services.AddSingleton<IAuthenticationClient, IntegrationAuthenticationClient>();
builder.Services.AddSingleton<IApplicationTokenRefreshClient, IntegrationApplicationTokenRefreshClient>();
builder.Services.AddSingleton<IAccessTokenService, AccessTokenService>();
builder.Services.AddSingleton<IIntegrationTokenStore, RedisIntegrationTokenStore>();
builder.Services.AddSingleton<IIntegrationTokenClient, Wso2IntegrationTokenClient>();
builder.Services.AddSingleton<IIntegrationTokenService, IntegrationTokenService>();
builder.Services.AddSingleton<IIntegrationClient, IntegrationClient>();
builder.Services.AddSingleton<IContentModerationService, OpenAiContentModerationService>();
builder.Services.AddSingleton<IGoogleAuthenticationService, GoogleAuthenticationService>();

var allowedOrigins = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()?.AllowedOrigins ?? [];
builder.Services.AddCors(options => options.AddPolicy("Angular", policy => policy
    .WithOrigins(allowedOrigins)
    .AllowCredentials()
    .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
    .AllowAnyHeader()));

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseCors("Angular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

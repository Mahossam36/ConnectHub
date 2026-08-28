using Ardalis.Result;
using ConnectHub.BLL.DTOs.Auth;
using ConnectHub.BLL.Interfaces.Services;
using ConnectHub.DAL.Interfaces;
using ConnectHub.Models.Entities;
using ConnectHub.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ConnectHub.BLL.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<User> userManager,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _configuration = configuration;
        _logger = logger;
    }



    public async Task<Result<AuthResponseDto>> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            return Result.Conflict("A user with this email address already exists.");

        var userId = Guid.NewGuid();

        var appUser = new User
        {
            Id = userId,
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName?.Trim() ?? string.Empty,
            LastName = request.LastName?.Trim() ?? string.Empty,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var identityResult = await _userManager.CreateAsync(appUser, request.Password);

        if (!identityResult.Succeeded)
        {
            var errors = identityResult.Errors
                .Select(e => new ValidationError(e.Description))
                .ToList();

            return Result.Invalid(errors);
        }

        await _auditService.LogAsync(
            "Register",
            "User",
            userId,
            userId,
            null,
            cancellationToken);

        _logger.LogInformation(
            "User {UserId} registered successfully.",
            userId);

        return await GenerateAuthResponseAsync(
            appUser,
            appUser.Email ?? request.Email,
            cancellationToken);
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var appUser = await _userManager.FindByEmailAsync(request.Email);

        if (appUser is null)
        {
            _logger.LogWarning("Failed login attempt for non-existent email.");
            return Result.Unauthorized("Invalid email or password.");
        }

        var isPasswordValid =
            await _userManager.CheckPasswordAsync(appUser, request.Password);

        if (!isPasswordValid)
        {
            _logger.LogWarning(
                "Failed login attempt for user {UserId}.",
                appUser.Id);

            return Result.Unauthorized("Invalid email or password.");
        }

        if (!appUser.IsActive)
        {
            _logger.LogWarning(
                "Login attempt for deactivated user {UserId}.",
                appUser.Id);

            return Result.Forbidden("Account is deactivated.");
        }

        await _auditService.LogAsync(
            "Login",
            "User",
            appUser.Id,
            appUser.Id,
            null,
            cancellationToken);

        _logger.LogInformation(
            "User {UserId} successfully authenticated.",
            appUser.Id);

        return await GenerateAuthResponseAsync(
            appUser,
            appUser.Email ?? request.Email,
            cancellationToken);
    }



    public async Task<Result<AuthResponseDto>> ExternalLoginAsync(
    ExternalLoginRequest request,
    CancellationToken cancellationToken = default)
    {
        var appUser = await _userManager.Users
            .FirstOrDefaultAsync(
                u => u.ExternalProvider == request.Provider &&
                     u.ExternalProviderId == request.ProviderId,
                cancellationToken);

        // Existing external user
        if (appUser is not null)
        {
            if (!appUser.IsActive)
            {
                return Result.Forbidden("Account is deactivated.");
            }

            await _auditService.LogAsync(
                "External Login",
                "User",
                appUser.Id,
                appUser.Id,
                null,
                cancellationToken);

            return await GenerateAuthResponseAsync(
                appUser,
                appUser.Email ?? string.Empty,
                cancellationToken);
        }

        // Email already belongs to another account
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var existingEmailUser =
                await _userManager.FindByEmailAsync(request.Email);

            if (existingEmailUser is not null)
            {
                return Result.Conflict(
                    "An account with this email already exists.");
            }
        }

        // Create new external user
        var newUser = new User
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName ?? string.Empty,
            LastName = request.LastName ?? string.Empty,
            ProfileImage = request.Provider == ExternalProvider.Local
                ? null
                : request.ProfileImageUrl,
            IsActive = true,
            ExternalProvider = request.Provider,
            ExternalProviderId = request.ProviderId,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(newUser);

        if (!createResult.Succeeded)
        {
            _logger.LogError(
                "Failed to create external user for provider {Provider}.",
                request.Provider);

            return Result.Error(
                string.Join("; ", createResult.Errors.Select(e => e.Description)));
        }

        await _auditService.LogAsync(
            "External Registration",
            "User",
            newUser.Id,
            newUser.Id,
            null,
            cancellationToken);

        return await GenerateAuthResponseAsync(
            newUser,
            newUser.Email ?? string.Empty,
            cancellationToken);
    }

    public async Task<Result<AuthResponseDto>> RefreshTokenAsync(
    RefreshTokenRequestDto request,
    CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(request.RefreshToken);

        var existingRefreshToken =
            await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

        if (existingRefreshToken is null)
        {
            _logger.LogWarning("Refresh token not found in database.");
            return Result.Unauthorized("Invalid refresh token.");
        }

        if (existingRefreshToken.IsRevoked)
        {
            _logger.LogWarning(
                "Attempt to reuse revoked refresh token for user {UserId}. " +
                "Revoking all active tokens.",
                existingRefreshToken.UserId);

            await _refreshTokenRepository.RevokeAllUserTokensAsync(
                existingRefreshToken.UserId,
                "Reused revoked token detected");

            await _unitOfWork.SaveChangesAsync();

            return Result.Unauthorized("Refresh token has been revoked.");
        }

        if (existingRefreshToken.IsExpired)
        {
            _logger.LogWarning(
                "Refresh token expired for user {UserId}.",
                existingRefreshToken.UserId);

            return Result.Unauthorized("Refresh token has expired.");
        }


        var user = await _userManager.FindByIdAsync(
            existingRefreshToken.UserId.ToString());

        if (user is null)
        {
            _logger.LogWarning(
                "User {UserId} associated with refresh token was not found.",
                existingRefreshToken.UserId);

            return Result.NotFound("User account not found.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning(
                "Refresh token used for inactive user {UserId}.",
                user.Id);

            return Result.Forbidden("User account is inactive.");
        }

        var email = user.Email ?? string.Empty;

        // Rotate the refresh token.
        existingRefreshToken.RevokedAt = DateTime.UtcNow;
        existingRefreshToken.RevokedReason = "Rotated";

        _refreshTokenRepository.Update(existingRefreshToken);

        var (accessToken, expiresAt) =
            GenerateJwtAccessToken(user, email);

        var (rawRefreshToken, newRefreshTokenEntity) =
            CreateRefreshTokenEntity(user.Id);

        await _refreshTokenRepository.AddAsync(newRefreshTokenEntity);

        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAsync(
            "RefreshToken",
            "User",
            user.Id,
            user.Id,
            null,
            cancellationToken);

        _logger.LogInformation(
            "Refresh token successfully rotated for user {UserId}.",
            user.Id);

        return Result.Success(new AuthResponseDto
        {
            UserId = user.Id,
            Email = email,
            DisplayName = $"{user.FirstName} {user.LastName}".Trim(),
            AvatarUrl = ToAvatarUrl(user.ProfileImage),
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresAt = expiresAt
        });
    }

    public async Task<Result> RevokeTokenAsync(
    RefreshTokenRequestDto request,
    Guid? currentUserId = null,
    CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(request.RefreshToken);

        var refreshToken =
            await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

        if (refreshToken is null)
            return Result.NotFound("Refresh token not found.");

        if (currentUserId.HasValue &&
            refreshToken.UserId != currentUserId.Value)
        {
            return Result.Forbidden(
                "You cannot revoke tokens belonging to another user.");
        }

        // Token is already revoked.
        if (refreshToken.IsRevoked)
            return Result.Success();

        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedReason = "Logged out / explicitly revoked";

        _refreshTokenRepository.Update(refreshToken);

        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAsync(
            "Logout",
            "User",
            refreshToken.UserId,
            refreshToken.UserId,
            null,
            cancellationToken);

        _logger.LogInformation(
            "Refresh token revoked for user {UserId}.",
            refreshToken.UserId);

        return Result.Success();
    }

    /// <summary>
    /// Common token generation pipeline used by both local authentication and Google SSO
    /// </summary>
    public async Task<Result<AuthResponseDto>> GenerateAuthResponseAsync(
    User user,
    string email,
    CancellationToken cancellationToken = default)
    {
        var (accessToken, expiresAt) =
            GenerateJwtAccessToken(user, email);

        var (rawRefreshToken, refreshTokenEntity) =
            CreateRefreshTokenEntity(user.Id);

        await _refreshTokenRepository.AddAsync(refreshTokenEntity);

        // Saves the new refresh token.
        await _unitOfWork.SaveChangesAsync();

        var displayName =
            $"{user.FirstName} {user.LastName}".Trim();

        return Result.Success(new AuthResponseDto
        {
            UserId = user.Id,
            Email = email,
            DisplayName = displayName,
            AvatarUrl = ToAvatarUrl(user.ProfileImage),
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresAt = expiresAt
        });
    }

    private (string token, DateTime expiresAt) GenerateJwtAccessToken(User user, string email)
    {
        var secret = _configuration["Jwt:Secret"] ?? _configuration["Jwt:Key"] ?? "ConnectHubDefaultSecretKeyForJwtTokenGenerationMustBeAtLeast32BytesLong!";
        var issuer = _configuration["Jwt:Issuer"] ?? "ConnectHub";
        var audience = _configuration["Jwt:Audience"] ?? "ConnectHubClients";
        var expiryMinutesStr = _configuration["Jwt:ExpiryMinutes"] ?? "60";
        var expiryMinutes = int.TryParse(expiryMinutesStr, out var mins) ? mins : 60;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var displayName = $"{user.FirstName} {user.LastName}".Trim();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, displayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return (tokenHandler.WriteToken(token), expiresAt);
    }

    private (string rawToken, RefreshToken entity) CreateRefreshTokenEntity(Guid userId)
    {
        var expiryDaysStr = _configuration["Jwt:RefreshTokenExpiryDays"] ?? "7";
        var expiryDays = int.TryParse(expiryDaysStr, out var days) ? days : 7;

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var tokenHash = HashToken(rawToken);

        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays)
        };

        return (rawToken, entity);
    }

    private static string HashToken(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static string? ToAvatarUrl(string? profileImage)
    {
        if (string.IsNullOrWhiteSpace(profileImage))
            return null;

        return Uri.TryCreate(profileImage, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? profileImage
            : $"/{profileImage.TrimStart('/')}";
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ardalis.Result;
using ConnectHub.BLL.DTOs.Auth;
using ConnectHub.BLL.Interfaces.Services;
using ConnectHub.DAL.Identity;
using ConnectHub.DAL.Interfaces;
using ConnectHub.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace ConnectHub.BLL.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IGenericRepository<User> _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IGenericRepository<User> userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _userRepository = userRepository;
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
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Result.Invalid(new ValidationError("Email and Password are required."));

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            return Result.Conflict("A user with this email address already exists.");

        var userId = Guid.NewGuid();

        var appUser = new ApplicationUser
        {
            Id = userId,
            UserName = request.Email,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow
        };

        var identityResult = await _userManager.CreateAsync(appUser, request.Password);
        if (!identityResult.Succeeded)
        {
            var errors = identityResult.Errors.Select(e => new ValidationError(e.Description)).ToList();
            return Result.Invalid(errors);
        }

        // Create domain profile with identical Id
        var domainUser = new User
        {
            Id = userId,
            FirstName = request.FirstName?.Trim() ?? string.Empty,
            LastName = request.LastName?.Trim() ?? string.Empty,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(domainUser);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAsync("Register", "User", userId, userId, null, cancellationToken);
        _logger.LogInformation("User {UserId} registered successfully.", userId);

        return await GenerateAuthResponseAsync(domainUser, appUser.Email ?? request.Email, cancellationToken);
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Result.Invalid(new ValidationError("Email and Password are required."));

        var appUser = await _userManager.FindByEmailAsync(request.Email);
        if (appUser is null)
        {
            _logger.LogWarning("Failed login attempt for non-existent email.");
            return Result.Unauthorized("Invalid email or password.");
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(appUser, request.Password);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Failed login attempt for user {UserId}.", appUser.Id);
            return Result.Unauthorized("Invalid email or password.");
        }

        var domainUser = await _userRepository.GetByIdAsync(appUser.Id);
        if (domainUser is null || !domainUser.IsActive)
        {
            _logger.LogWarning("Login attempt for deactivated user {UserId}.", appUser.Id);
            return Result.Forbidden("Account is deactivated or profile not found.");
        }

        await _auditService.LogAsync("Login", "User", appUser.Id, appUser.Id, null, cancellationToken);
        _logger.LogInformation("User {UserId} successfully authenticated.", appUser.Id);

        return await GenerateAuthResponseAsync(domainUser, appUser.Email ?? request.Email, cancellationToken);
    }

    public async Task<Result<AuthResponseDto>> RefreshTokenAsync(
        RefreshTokenRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Result.Invalid(new ValidationError("Refresh token is required."));

        var tokenHash = HashToken(request.RefreshToken);
        var existingRefreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

        if (existingRefreshToken is null)
        {
            _logger.LogWarning("Refresh token not found in database.");
            return Result.Unauthorized("Invalid refresh token.");
        }

        if (existingRefreshToken.IsRevoked)
        {
            _logger.LogWarning("Attempt to reuse revoked refresh token for user {UserId}. Revoking all active tokens.", existingRefreshToken.UserId);
            // Security measure: revoke all active tokens for this user upon reuse of revoked token
            await _refreshTokenRepository.RevokeAllUserTokensAsync(existingRefreshToken.UserId, "Reused revoked token detected");
            await _unitOfWork.SaveChangesAsync();
            return Result.Unauthorized("Refresh token has been revoked.");
        }

        if (existingRefreshToken.IsExpired)
        {
            _logger.LogWarning("Refresh token expired for user {UserId}.", existingRefreshToken.UserId);
            return Result.Unauthorized("Refresh token has expired.");
        }

        var domainUser = await _userRepository.GetByIdAsync(existingRefreshToken.UserId);
        if (domainUser is null || !domainUser.IsActive)
            return Result.Forbidden("User account is inactive.");

        var appUser = await _userManager.FindByIdAsync(existingRefreshToken.UserId.ToString());
        var email = appUser?.Email ?? string.Empty;

        // Revoke the old token (rotation)
        existingRefreshToken.RevokedAt = DateTime.UtcNow;
        existingRefreshToken.RevokedReason = "Rotated";
        _refreshTokenRepository.Update(existingRefreshToken);

        var (accessToken, expiresAt) = GenerateJwtAccessToken(domainUser, email);
        var (rawRefreshToken, newRefreshTokenEntity) = CreateRefreshTokenEntity(domainUser.Id);

        await _refreshTokenRepository.AddAsync(newRefreshTokenEntity);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAsync("RefreshToken", "User", domainUser.Id, domainUser.Id, null, cancellationToken);
        _logger.LogInformation("Refresh token successfully rotated for user {UserId}.", domainUser.Id);

        var displayName = $"{domainUser.FirstName} {domainUser.LastName}".Trim();

        return Result.Success(new AuthResponseDto
        {
            UserId = domainUser.Id,
            Email = email,
            DisplayName = displayName,
            AvatarUrl = domainUser.ProfileImagePath,
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
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Result.Invalid(new ValidationError("Refresh token is required."));

        var tokenHash = HashToken(request.RefreshToken);
        var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

        if (refreshToken is null)
            return Result.NotFound("Refresh token not found.");

        if (currentUserId.HasValue && refreshToken.UserId != currentUserId.Value)
            return Result.Forbidden("You cannot revoke tokens belonging to another user.");

        if (!refreshToken.IsRevoked)
        {
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedReason = "Logged out / explicitly revoked";
            _refreshTokenRepository.Update(refreshToken);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogAsync("Logout", "User", refreshToken.UserId, refreshToken.UserId, null, cancellationToken);
            _logger.LogInformation("Refresh token revoked for user {UserId}.", refreshToken.UserId);
        }

        return Result.Success();
    }

    /// <summary>
    /// Common token generation pipeline used by both local authentication and future Google SSO.
    /// </summary>
    public async Task<Result<AuthResponseDto>> GenerateAuthResponseAsync(
        User domainUser,
        string email,
        CancellationToken cancellationToken = default)
    {
        var (accessToken, expiresAt) = GenerateJwtAccessToken(domainUser, email);
        var (rawRefreshToken, refreshTokenEntity) = CreateRefreshTokenEntity(domainUser.Id);

        await _refreshTokenRepository.AddAsync(refreshTokenEntity);
        await _unitOfWork.SaveChangesAsync();

        var displayName = $"{domainUser.FirstName} {domainUser.LastName}".Trim();

        return Result.Success(new AuthResponseDto
        {
            UserId = domainUser.Id,
            Email = email,
            DisplayName = displayName,
            AvatarUrl = domainUser.ProfileImagePath,
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
}

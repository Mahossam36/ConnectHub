using Ardalis.Result;
using ConnectHub.BLL.DTOs.Auth;

namespace ConnectHub.BLL.Interfaces.Services;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default);
    Task<Result> RevokeTokenAsync(RefreshTokenRequestDto request, Guid? currentUserId = null, CancellationToken cancellationToken = default);
}

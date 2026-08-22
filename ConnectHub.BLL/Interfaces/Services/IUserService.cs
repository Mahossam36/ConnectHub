using Ardalis.Result;
using ConnectHub.BLL.DTOs.Users;

namespace ConnectHub.BLL.Interfaces.Services;

public interface IUserService
{
    Task<Result<UserProfileResponseDto>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<UserProfileResponseDto>> UpdateProfileAsync(Guid currentUserId, UpdateProfileRequestDto request, CancellationToken cancellationToken = default);
    Task<Result<UserProfileResponseDto>> UpdateAvatarAsync(Guid currentUserId, Stream imageStream, string fileName, CancellationToken cancellationToken = default);
}

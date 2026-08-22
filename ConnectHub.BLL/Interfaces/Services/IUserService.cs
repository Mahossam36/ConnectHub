using ConnectHub.BLL.DTOs.Users;

namespace ConnectHub.BLL.Interfaces.Services;

public interface IUserService
{
    Task<UserProfileResponseDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserProfileResponseDto> UpdateProfileAsync(Guid currentUserId, UpdateProfileRequestDto request, CancellationToken cancellationToken = default);
    Task<UserProfileResponseDto> UpdateAvatarAsync(Guid currentUserId, Stream imageStream, string fileName, CancellationToken cancellationToken = default);
}

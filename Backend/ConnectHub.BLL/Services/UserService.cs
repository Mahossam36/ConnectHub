using Ardalis.Result;
using AutoMapper;
using ConnectHub.BLL.DTOs.Users;
using ConnectHub.BLL.Interfaces.Services;
using ConnectHub.BLL.Interfaces.Storage;
using ConnectHub.DAL.Interfaces;
using ConnectHub.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace ConnectHub.BLL.Services;

public class UserService : IUserService
{
    private readonly IGenericRepository<User> _userRepository;
    private readonly UserManager<User> _userManager;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAuditService _auditService;
    private readonly IXssSanitizerService _xssSanitizer;
    private readonly IContentModerationService _contentModeration;
    private readonly ILogger<UserService> _logger;

    private const string AvatarFolder = "uploads/profile-images";

    public UserService(
        IGenericRepository<User> userRepository,
        UserManager<User> userManager,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IAuditService auditService,
        IXssSanitizerService xssSanitizer,
        IContentModerationService contentModeration,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _auditService = auditService;
        _xssSanitizer = xssSanitizer;
        _contentModeration = contentModeration;
        _logger = logger;
    }

    public async Task<Result<UserProfileResponseDto>> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return Result.NotFound($"User with ID '{userId}' was not found.");

        var appUser = await _userManager.FindByIdAsync(userId.ToString());
        var dto = _mapper.Map<UserProfileResponseDto>(user);
        dto.Email = appUser?.Email;

        return Result.Success(dto);
    }

    public async Task<Result<UserProfileResponseDto>> UpdateProfileAsync(
        Guid currentUserId,
        UpdateProfileRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(currentUserId);
        if (user is null)
            return Result.NotFound($"User with ID '{currentUserId}' was not found.");

        var firstName = _xssSanitizer.Sanitize(request.FirstName);
        var lastName = _xssSanitizer.Sanitize(request.LastName);
        var bio = _xssSanitizer.Sanitize(request.Bio);
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            return Result.Invalid(new ValidationError("First name and last name are required."));

        var moderationResult = await _contentModeration.IsContentSafeAsync(
            $"{firstName} {lastName} {bio}", cancellationToken);
        if (!moderationResult.IsSuccess)
            return Result.Invalid(moderationResult.ValidationErrors);

        user.FirstName = firstName;
        user.LastName = lastName;
        user.Bio = string.IsNullOrWhiteSpace(bio) ? null : bio;
        user.UpdatedAt = DateTime.UtcNow;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAsync("UpdateProfile", "User", user.Id, currentUserId, null, cancellationToken);
        _logger.LogInformation("Profile updated for user {UserId}.", currentUserId);

        var appUser = await _userManager.FindByIdAsync(currentUserId.ToString());
        var dto = _mapper.Map<UserProfileResponseDto>(user);
        dto.Email = appUser?.Email;

        return Result.Success(dto);
    }

    public async Task<Result<UserProfileResponseDto>> UpdateAvatarAsync(
        Guid currentUserId,
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        if (imageStream is null || imageStream.Length == 0)
            return Result.Invalid(new ValidationError("Image stream cannot be null or empty."));

        var user = await _userRepository.GetByIdAsync(currentUserId);
        if (user is null)
            return Result.NotFound($"User with ID '{currentUserId}' was not found.");

        var extension = Path.GetExtension(fileName);
        var storedFileName = $"profile{extension}";
        var userAvatarFolder = $"{AvatarFolder}/{currentUserId}";

        // External profile images are URLs and must never be used as local file-system paths.
        if (IsLocalProfileImage(user.ProfileImage))
        {
            await _fileStorageService.DeleteFileAsync(user.ProfileImage!);
        }

        var relativePath = await _fileStorageService.SaveFileAsync(imageStream, storedFileName, userAvatarFolder);
        user.ProfileImage = relativePath;
        user.UpdatedAt = DateTime.UtcNow;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogAsync("UpdateAvatar", "User", user.Id, currentUserId, null, cancellationToken);
        _logger.LogInformation("Avatar updated for user {UserId}.", currentUserId);

        var appUser = await _userManager.FindByIdAsync(currentUserId.ToString());
        var dto = _mapper.Map<UserProfileResponseDto>(user);
        dto.Email = appUser?.Email;

        return Result.Success(dto);
    }

    private static bool IsLocalProfileImage(string? profileImage) =>
        !string.IsNullOrWhiteSpace(profileImage) &&
        (!Uri.TryCreate(profileImage, UriKind.Absolute, out var uri) ||
         (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps));
}

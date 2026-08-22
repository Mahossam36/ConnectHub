using ConnectHub.BLL.DTOs.Users;
using ConnectHub.BLL.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectHub.API.Controllers;

[Route("api/[controller]")]
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Get profile of any user by ID.
    /// </summary>
    [HttpGet("{id:guid}/profile")]
    [ProducesResponseType(typeof(UserProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponseDto>> GetProfile(Guid id, CancellationToken cancellationToken)
    {
        var result = await _userService.GetProfileAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Get profile of currently authenticated user.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponseDto>> GetCurrentProfile(CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _userService.GetProfileAsync(currentUserId, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Update profile information of the current user.
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponseDto>> UpdateProfile([FromBody] UpdateProfileRequestDto request, CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _userService.UpdateProfileAsync(currentUserId, request, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Upload and update avatar for the current user.
    /// </summary>
    [HttpPost("avatar")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponseDto>> UpdateAvatar(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ProblemDetails { Status = 400, Title = "File is required." });

        var currentUserId = GetRequiredUserId();
        await using var stream = file.OpenReadStream();
        var result = await _userService.UpdateAvatarAsync(currentUserId, stream, file.FileName, cancellationToken);
        return ToActionResult(result);
    }
}

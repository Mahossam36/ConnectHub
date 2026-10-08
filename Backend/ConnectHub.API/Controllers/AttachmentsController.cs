using ConnectHub.BLL.DTOs.Attachments;
using ConnectHub.BLL.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectHub.API.Controllers;

[Route("api/[controller]")]
public class AttachmentsController : BaseApiController
{
    private readonly IAttachmentService _attachmentService;

    public AttachmentsController(IAttachmentService attachmentService)
    {
        _attachmentService = attachmentService;
    }

    /// <summary>
    /// Upload a media file/attachment for a post.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(AttachmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AttachmentResponseDto>> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ProblemDetails { Status = 400, Title = "File is required." });

        var currentUserId = GetRequiredUserId();
        await using var stream = file.OpenReadStream();

        var result = await _attachmentService.UploadAsync(
            currentUserId,
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            cancellationToken);

        return ToCreatedResult(result);
    }

    /// <summary>
    /// Delete an uploaded attachment (Uploader only).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredUserId();
        var result = await _attachmentService.DeleteAsync(id, currentUserId, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Stream/download an uploaded attachment file by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(Guid id, CancellationToken cancellationToken)
    {
        var result = await _attachmentService.GetFileAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Status == Ardalis.Result.ResultStatus.NotFound
                ? NotFound(new ProblemDetails { Status = 404, Title = "Attachment not found." })
                : BadRequest(new ProblemDetails { Status = 400, Title = string.Join("; ", result.Errors) });
        }

        var (stream, contentType, fileName) = result.Value;
        return File(stream, contentType, fileName, enableRangeProcessing: true);
    }
}

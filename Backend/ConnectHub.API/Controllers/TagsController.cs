using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Groups;
using ConnectHub.BLL.DTOs.Tags;
using ConnectHub.BLL.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectHub.API.Controllers;

[Route("api/[controller]")]
public class TagsController : BaseApiController
{
    private readonly ITagService _tagService;

    public TagsController(ITagService tagService)
    {
        _tagService = tagService;
    }

    /// <summary>Gets tags available for group discovery and creation.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<TagDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<TagDto>>> GetTags(
        [FromQuery] string? search,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var pagination = new PaginationParams { Skip = skip, Take = take };
        var result = await _tagService.GetTagsAsync(search, pagination, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>Creates a tag for group discovery and organization.</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(TagDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TagDto>> CreateTag(
        [FromBody] CreateTagRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _tagService.CreateTagAsync(request, cancellationToken);
        return ToCreatedResult(result);
    }
}

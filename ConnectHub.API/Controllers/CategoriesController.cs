using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Categories;
using ConnectHub.BLL.DTOs.Groups;
using ConnectHub.BLL.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectHub.API.Controllers;

[Route("api/[controller]")]
public class CategoriesController : BaseApiController
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>Gets all application-managed group categories.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<CategoryDto>>> GetCategories(
        [FromQuery] string? search,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var pagination = new PaginationParams { Skip = skip, Take = take };
        var result = await _categoryService.GetCategoriesAsync(search, pagination, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>Creates a category for organizing community groups.</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> CreateCategory(
        [FromBody] CreateCategoryRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.CreateCategoryAsync(request, cancellationToken);
        return ToCreatedResult(result);
    }
}

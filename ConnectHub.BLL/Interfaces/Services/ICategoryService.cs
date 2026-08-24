using Ardalis.Result;
using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Categories;
using ConnectHub.BLL.DTOs.Groups;

namespace ConnectHub.BLL.Interfaces.Services;

public interface ICategoryService
{
    Task<Result<PagedResultDto<CategoryDto>>> GetCategoriesAsync( string? search, PaginationParams pagination,CancellationToken cancellationToken = default);
    Task<Result<CategoryDto>> CreateCategoryAsync(CreateCategoryRequestDto request, CancellationToken cancellationToken = default);
}

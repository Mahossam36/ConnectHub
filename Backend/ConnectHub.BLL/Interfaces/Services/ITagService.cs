using Ardalis.Result;
using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Groups;
using ConnectHub.BLL.DTOs.Tags;

namespace ConnectHub.BLL.Interfaces.Services;

public interface ITagService
{
    Task<Result<PagedResultDto<TagDto>>> GetTagsAsync(string? search, PaginationParams pagination, CancellationToken cancellationToken = default);
    Task<Result<TagDto>> CreateTagAsync(CreateTagRequestDto request, CancellationToken cancellationToken = default);
}

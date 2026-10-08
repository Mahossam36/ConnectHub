using Ardalis.Result;
using AutoMapper;
using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Groups;
using ConnectHub.BLL.DTOs.Tags;
using ConnectHub.BLL.Interfaces.Services;
using ConnectHub.DAL.Interfaces;
using ConnectHub.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConnectHub.BLL.Services;

public class TagService : ITagService
{
    private readonly IGenericRepository<Tag> _tagRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IXssSanitizerService _xssSanitizer;
    private readonly ILogger<TagService> _logger;

    public TagService(
        IGenericRepository<Tag> tagRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IXssSanitizerService xssSanitizer,
        ILogger<TagService> logger)
    {
        _tagRepository = tagRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _xssSanitizer = xssSanitizer;
        _logger = logger;
    }

    public async Task<Result<PagedResultDto<TagDto>>> GetTagsAsync(
        string? search,
        PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        var query = _tagRepository.Query();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(tag => tag.Name.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var tags = await query
            .OrderBy(tag => tag.Name)
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .Select(tag => new TagDto
            {
                Id = tag.Id,
                Name = tag.Name
            })
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResultDto<TagDto>(tags, total, pagination.Skip, pagination.Take));
    }

    public async Task<Result<TagDto>> CreateTagAsync(
        CreateTagRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var name = _xssSanitizer.Sanitize(request.Name).Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Result.Invalid(new ValidationError("Tag name is required."));

        var alreadyExists = await _tagRepository.ExistsAsync(tag => tag.Name == name);
        if (alreadyExists)
            return Result.Conflict("A tag with this name already exists.");

        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            Name = name,
            GroupCount = 0
        };

        await _tagRepository.AddAsync(tag);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Result.Conflict("A tag with this name already exists.");
        }

        _logger.LogInformation("Tag {TagId} created.", tag.Id);
        return Result.Success(_mapper.Map<TagDto>(tag));
    }
}

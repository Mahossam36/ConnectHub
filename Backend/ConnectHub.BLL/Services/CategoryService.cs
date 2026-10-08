using Ardalis.Result;
using AutoMapper;
using ConnectHub.BLL.Common.Pagination;
using ConnectHub.BLL.DTOs.Categories;
using ConnectHub.BLL.DTOs.Groups;
using ConnectHub.BLL.Interfaces.Services;
using ConnectHub.DAL.Interfaces;
using ConnectHub.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConnectHub.BLL.Services;

public class CategoryService : ICategoryService
{
    private readonly IGenericRepository<Category> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IXssSanitizerService _xssSanitizer;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        IGenericRepository<Category> categoryRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IXssSanitizerService xssSanitizer,
        ILogger<CategoryService> logger)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _xssSanitizer = xssSanitizer;
        _logger = logger;
    }

    public async Task<Result<PagedResultDto<CategoryDto>>> GetCategoriesAsync(
    string? search,
    PaginationParams pagination,
    CancellationToken cancellationToken = default)
    {
        var query = _categoryRepository.Query();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(category =>
                category.Name.Contains(search));
        }

        var total = await query.CountAsync(cancellationToken);

        var categories = await query
            .OrderBy(category => category.Name)
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .Select(category => new CategoryDto
            {
                Id = category.Id,
                Name = category.Name
            })
            .ToListAsync(cancellationToken);

        var result = new PagedResultDto<CategoryDto>(categories, total,pagination.Skip, pagination.Take);

        return Result.Success(result);
    }

    public async Task<Result<CategoryDto>> CreateCategoryAsync(
        CreateCategoryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var name = _xssSanitizer.Sanitize(request.Name).Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Result.Invalid(new ValidationError("Category name is required."));

        var alreadyExists = await _categoryRepository.ExistsAsync(category => category.Name == name);
        if (alreadyExists)
            return Result.Conflict("A category with this name already exists.");

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            GroupCount = 0
        };

        await _categoryRepository.AddAsync(category);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Result.Conflict("A category with this name already exists.");
        }

        _logger.LogInformation("Category {CategoryId} created.", category.Id);
        return Result.Success(_mapper.Map<CategoryDto>(category));
    }
}

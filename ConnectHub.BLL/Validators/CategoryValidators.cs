using ConnectHub.BLL.DTOs.Categories;
using FluentValidation;

namespace ConnectHub.BLL.Validators;

public class CreateCategoryRequestDtoValidator : AbstractValidator<CreateCategoryRequestDto>
{
    public CreateCategoryRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Category name is required.")
            .MaximumLength(100)
            .WithMessage("Category name cannot exceed 100 characters.");
    }
}

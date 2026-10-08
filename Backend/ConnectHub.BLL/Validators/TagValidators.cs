using ConnectHub.BLL.DTOs.Tags;
using FluentValidation;

namespace ConnectHub.BLL.Validators;

public class CreateTagRequestDtoValidator : AbstractValidator<CreateTagRequestDto>
{
    public CreateTagRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Tag name is required.")
            .MaximumLength(50)
            .WithMessage("Tag name cannot exceed 50 characters.");
    }
}

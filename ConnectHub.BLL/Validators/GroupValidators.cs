using ConnectHub.BLL.DTOs.Groups;
using FluentValidation;

namespace ConnectHub.BLL.Validators;

public class CreateGroupRequestDtoValidator : AbstractValidator<CreateGroupRequestDto>
{
    public CreateGroupRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Group name is required.")
            .MinimumLength(3).WithMessage("Group name must be at least 3 characters.")
            .MaximumLength(100).WithMessage("Group name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("CategoryId is required.");
    }
}

public class UpdateGroupRequestDtoValidator : AbstractValidator<UpdateGroupRequestDto>
{
    public UpdateGroupRequestDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Group name is required.")
            .MinimumLength(3).WithMessage("Group name must be at least 3 characters.")
            .MaximumLength(100).WithMessage("Group name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("CategoryId is required.");
    }
}

public class ChangeMemberRoleRequestDtoValidator : AbstractValidator<ChangeMemberRoleRequestDto>
{
    public ChangeMemberRoleRequestDtoValidator()
    {
        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("A valid group role must be specified.");
    }
}

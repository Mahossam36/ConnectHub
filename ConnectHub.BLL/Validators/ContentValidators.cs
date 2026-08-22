using ConnectHub.BLL.DTOs.Comments;
using ConnectHub.BLL.DTOs.Posts;
using ConnectHub.BLL.DTOs.Reports;
using ConnectHub.BLL.DTOs.Users;
using FluentValidation;

namespace ConnectHub.BLL.Validators;

public class CreatePostRequestDtoValidator : AbstractValidator<CreatePostRequestDto>
{
    public CreatePostRequestDtoValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Post content is required.")
            .MaximumLength(10000).WithMessage("Post content cannot exceed 10,000 characters.");
    }
}

public class UpdatePostRequestDtoValidator : AbstractValidator<UpdatePostRequestDto>
{
    public UpdatePostRequestDtoValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Post content is required.")
            .MaximumLength(10000).WithMessage("Post content cannot exceed 10,000 characters.");
    }
}

public class CreateCommentRequestDtoValidator : AbstractValidator<CreateCommentRequestDto>
{
    public CreateCommentRequestDtoValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Comment content is required.")
            .MaximumLength(2000).WithMessage("Comment content cannot exceed 2,000 characters.");
    }
}

public class UpdateCommentRequestDtoValidator : AbstractValidator<UpdateCommentRequestDto>
{
    public UpdateCommentRequestDtoValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Comment content is required.")
            .MaximumLength(2000).WithMessage("Comment content cannot exceed 2,000 characters.");
    }
}

public class UpdateProfileRequestDtoValidator : AbstractValidator<UpdateProfileRequestDto>
{
    public UpdateProfileRequestDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Bio)
            .MaximumLength(500).WithMessage("Bio cannot exceed 500 characters.");
    }
}

public class CreateReportRequestDtoValidator : AbstractValidator<CreateReportRequestDto>
{
    public CreateReportRequestDtoValidator()
    {
        RuleFor(x => x.TargetType)
            .IsInEnum().WithMessage("A valid target type (Post or Comment) must be specified.");

        RuleFor(x => x.TargetId)
            .NotEmpty().WithMessage("TargetId is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Report reason is required.")
            .MinimumLength(5).WithMessage("Reason must be at least 5 characters.")
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");
    }
}

public class ResolveReportRequestDtoValidator : AbstractValidator<ResolveReportRequestDto>
{
    public ResolveReportRequestDtoValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("A valid resolution status must be specified.");
    }
}

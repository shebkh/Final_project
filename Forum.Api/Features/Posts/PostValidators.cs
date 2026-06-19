// Forum.Api/Features/Posts/PostValidators.cs
using FluentValidation;

namespace Forum.Api.Features.Posts;

public sealed class CreatePostRequestValidator : AbstractValidator<CreatePostRequest>
{
    public CreatePostRequestValidator()
    {
        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Reply body is required.")
            .Length(2, 10_000).WithMessage("Reply must be between 2 and 10,000 characters.");
    }
}

public sealed class UpdatePostRequestValidator : AbstractValidator<UpdatePostRequest>
{
    public UpdatePostRequestValidator()
    {
        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Reply body is required.")
            .Length(2, 10_000).WithMessage("Reply must be between 2 and 10,000 characters.");
    }
}

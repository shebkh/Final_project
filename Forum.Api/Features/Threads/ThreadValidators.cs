// Forum.Api/Features/Threads/ThreadValidators.cs
using FluentValidation;

namespace Forum.Api.Features.Threads;

public sealed class CreateThreadRequestValidator : AbstractValidator<CreateThreadRequest>
{
    public CreateThreadRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .Length(5, 200).WithMessage("Title must be between 5 and 200 characters.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Body is required.")
            .Length(10, 10_000).WithMessage("Body must be between 10 and 10,000 characters.");
    }
}

public sealed class UpdateThreadRequestValidator : AbstractValidator<UpdateThreadRequest>
{
    public UpdateThreadRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .Length(5, 200).WithMessage("Title must be between 5 and 200 characters.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Body is required.")
            .Length(10, 10_000).WithMessage("Body must be between 10 and 10,000 characters.");
    }
}

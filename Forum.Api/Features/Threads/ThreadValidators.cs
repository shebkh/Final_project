// Forum.Api/Features/Threads/ThreadValidators.cs
using System.Text.RegularExpressions;
using FluentValidation;

namespace Forum.Api.Features.Threads;

public sealed partial class CreateThreadRequestValidator : AbstractValidator<CreateThreadRequest>
{
    public CreateThreadRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .Length(5, 200).WithMessage("Title must be between 5 and 200 characters.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Body is required.")
            .Length(10, 10_000).WithMessage("Body must be between 10 and 10,000 characters.");

        RuleFor(x => x.Tags)
            .Must(t => t is null || t.Count <= 5)
            .WithMessage("A thread can have at most 5 tags.");

        RuleForEach(x => x.Tags)
            .Must(TagRules.IsValid)
            .WithMessage("Each tag must be 2–25 characters: letters, digits, spaces, or hyphens.");
    }
}

public sealed partial class UpdateThreadRequestValidator : AbstractValidator<UpdateThreadRequest>
{
    public UpdateThreadRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .Length(5, 200).WithMessage("Title must be between 5 and 200 characters.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Body is required.")
            .Length(10, 10_000).WithMessage("Body must be between 10 and 10,000 characters.");

        RuleFor(x => x.Tags)
            .Must(t => t is null || t.Count <= 5)
            .WithMessage("A thread can have at most 5 tags.");

        RuleForEach(x => x.Tags)
            .Must(TagRules.IsValid)
            .WithMessage("Each tag must be 2–25 characters: letters, digits, spaces, or hyphens.");
    }
}

/// <summary>
/// Shared shape rule for a single raw tag. The service normalizes further
/// (trim, lowercase, spaces → hyphens, dedupe) — this only rejects garbage.
/// </summary>
internal static partial class TagRules
{
    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9 -]{1,24}$")]
    private static partial Regex TagShape();

    public static bool IsValid(string? tag) =>
        tag is not null && TagShape().IsMatch(tag.Trim());
}

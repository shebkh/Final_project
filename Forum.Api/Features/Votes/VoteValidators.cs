// Forum.Api/Features/Votes/VoteValidators.cs
using FluentValidation;

namespace Forum.Api.Features.Votes;

public sealed class CastVoteRequestValidator : AbstractValidator<CastVoteRequest>
{
    public CastVoteRequestValidator()
    {
        RuleFor(x => x.Value)
            .Must(v => v == 1 || v == -1)
            .WithMessage("Vote value must be +1 (up) or -1 (down).");
    }
}

// Forum.Api/Features/Votes/VotesModule.cs
using FluentValidation;

namespace Forum.Api.Features.Votes;

/// <summary>
/// Registers all services that belong to the Votes vertical slice.
/// Called from Program.cs so feature wiring stays inside the feature folder.
/// </summary>
public static class VotesModule
{
    public static IServiceCollection AddVotesFeature(this IServiceCollection services)
    {
        services.AddScoped<IVoteRepository, VoteRepository>();
        services.AddScoped<IVoteService, VoteService>();

        services.AddScoped<IValidator<CastVoteRequest>, CastVoteRequestValidator>();

        return services;
    }
}

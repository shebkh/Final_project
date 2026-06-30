// Forum.Api/Features/Moderation/ModerationModule.cs
namespace Forum.Api.Features.Moderation;

/// <summary>
/// Registers all services that belong to the Moderation vertical slice.
/// Called from Program.cs so feature wiring stays inside the feature folder.
/// The pin/lock requests are plain booleans (no FluentValidation needed).
/// </summary>
public static class ModerationModule
{
    public static IServiceCollection AddModerationFeature(this IServiceCollection services)
    {
        services.AddScoped<IModerationRepository, ModerationRepository>();
        services.AddScoped<IModerationService, ModerationService>();

        return services;
    }
}

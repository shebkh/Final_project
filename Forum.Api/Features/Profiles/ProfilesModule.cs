// Forum.Api/Features/Profiles/ProfilesModule.cs
namespace Forum.Api.Features.Profiles;

/// <summary>
/// Registers all services that belong to the Profiles vertical slice.
/// Called from Program.cs so feature wiring stays inside the feature folder.
/// Profiles are anonymous public reads — no validators (no request bodies).
/// </summary>
public static class ProfilesModule
{
    public static IServiceCollection AddProfilesFeature(this IServiceCollection services)
    {
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IProfileService, ProfileService>();

        return services;
    }
}

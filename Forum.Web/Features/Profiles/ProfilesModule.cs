// Forum.Web/Features/Profiles/ProfilesModule.cs
using Forum.Web.Features.Auth;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Forum.Web.Features.Profiles;

/// <summary>
/// Registers the client-side Profiles slice: the typed HttpClient for the API's
/// public user-profile endpoints. Profiles are anonymous reads; the shared
/// AuthTokenHandler is attached for consistency and is a no-op without a token.
/// </summary>
public static class ProfilesModule
{
    public static IServiceCollection AddProfilesFeature(this IServiceCollection services, IConfiguration config)
    {
        var apiBaseAddress = config["ApiBaseAddress"]
            ?? throw new InvalidOperationException("Missing 'ApiBaseAddress' configuration value.");

        // Ensure the bearer handler is available even if this slice is wired
        // without the Auth module. TryAdd is a no-op when Auth already registered it.
        services.TryAddTransient<AuthTokenHandler>();

        services.AddHttpClient<IProfileApiClient, ProfileApiClient>(client =>
        {
            client.BaseAddress = new Uri(apiBaseAddress);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler<AuthTokenHandler>();

        return services;
    }
}

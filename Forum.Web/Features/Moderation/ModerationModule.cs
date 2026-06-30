// Forum.Web/Features/Moderation/ModerationModule.cs
using Forum.Web.Features.Auth;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Forum.Web.Features.Moderation;

/// <summary>
/// Registers the client-side Moderation slice: the typed HttpClient for the API's
/// moderation endpoints, wired with the shared AuthTokenHandler so the moderator
/// bearer token is attached to pin/lock requests.
/// </summary>
public static class ModerationModule
{
    public static IServiceCollection AddModerationFeature(this IServiceCollection services, IConfiguration config)
    {
        var apiBaseAddress = config["ApiBaseAddress"]
            ?? throw new InvalidOperationException("Missing 'ApiBaseAddress' configuration value.");

        // Ensure the bearer handler is available even if this slice is wired
        // without the Auth module. TryAdd is a no-op when Auth already registered it.
        services.TryAddTransient<AuthTokenHandler>();

        services.AddHttpClient<IModerationApiClient, ModerationApiClient>(client =>
        {
            client.BaseAddress = new Uri(apiBaseAddress);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler<AuthTokenHandler>();

        return services;
    }
}

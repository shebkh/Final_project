// Forum.Web/Features/Notifications/NotificationsModule.cs
using Forum.Web.Features.Auth;
using Microsoft.AspNetCore.Components.Authorization;

namespace Forum.Web.Features.Notifications;

/// <summary>
/// Registers the client-side Notifications slice: the per-circuit SignalR
/// connection to the API's notification hub. The hub lives on the API host,
/// derived from the same ApiBaseAddress the typed HttpClients use.
/// </summary>
public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsFeature(this IServiceCollection services, IConfiguration config)
    {
        var apiBaseAddress = config["ApiBaseAddress"]
            ?? throw new InvalidOperationException("Missing 'ApiBaseAddress' configuration value.");

        var hubUrl = new Uri(new Uri(apiBaseAddress), "/hubs/notifications").ToString();

        services.AddScoped<ISignalRService>(sp => new SignalRService(
            hubUrl,
            sp.GetRequiredService<ITokenStore>(),
            sp.GetRequiredService<AuthenticationStateProvider>(),
            sp.GetRequiredService<ILogger<SignalRService>>()));

        return services;
    }
}

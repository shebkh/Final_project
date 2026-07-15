// Forum.Api/Features/Notifications/NotificationsModule.cs
namespace Forum.Api.Features.Notifications;

/// <summary>
/// Registers the Notifications vertical slice: SignalR plus the push service
/// other slices (Posts, Votes) call after a successful write.
/// </summary>
public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsFeature(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddScoped<INotificationService, NotificationService>();
        return services;
    }

    public static WebApplication MapNotificationsFeature(this WebApplication app)
    {
        app.MapHub<NotificationHub>(NotificationHub.Route);
        return app;
    }
}

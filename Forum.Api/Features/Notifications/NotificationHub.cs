// Forum.Api/Features/Notifications/NotificationHub.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Forum.Api.Features.Notifications;

/// <summary>
/// Server→client push channel for per-user notifications. Clients never invoke
/// hub methods; the API sends through <see cref="INotificationService"/> using
/// Clients.User(id) — the user id comes from the JWT NameIdentifier claim via
/// the default IUserIdProvider.
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    public const string Route = "/hubs/notifications";

    /// <summary>Client-side handler name invoked for every pushed notification.</summary>
    public const string ClientMethod = "Notify";
}

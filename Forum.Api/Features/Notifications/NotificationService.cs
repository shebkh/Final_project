// Forum.Api/Features/Notifications/NotificationService.cs
using Microsoft.AspNetCore.SignalR;

namespace Forum.Api.Features.Notifications;

public sealed class NotificationService(
    IHubContext<NotificationHub> hubContext,
    ILogger<NotificationService> logger) : INotificationService
{
    public Task NotifyThreadRepliedAsync(
        int recipientUserId, string replierUserName, int threadId, string threadTitle,
        CancellationToken ct = default) =>
        SendAsync(recipientUserId, new NotificationMessage(
            "reply",
            $"{replierUserName} replied to your thread “{threadTitle}”.",
            $"/threads/{threadId}",
            DateTime.UtcNow), ct);

    public Task NotifyThreadVotedAsync(
        int recipientUserId, int value, int threadId, string threadTitle,
        CancellationToken ct = default) =>
        SendAsync(recipientUserId, new NotificationMessage(
            "thread-vote",
            $"Your thread “{threadTitle}” received {VoteWord(value)}.",
            $"/threads/{threadId}",
            DateTime.UtcNow), ct);

    public Task NotifyPostVotedAsync(
        int recipientUserId, int value, int threadId, string threadTitle,
        CancellationToken ct = default) =>
        SendAsync(recipientUserId, new NotificationMessage(
            "post-vote",
            $"Your reply in “{threadTitle}” received {VoteWord(value)}.",
            $"/threads/{threadId}",
            DateTime.UtcNow), ct);

    private async Task SendAsync(int recipientUserId, NotificationMessage message, CancellationToken ct)
    {
        try
        {
            // Targets every active connection of that user; a no-op when offline.
            await hubContext.Clients.User(recipientUserId.ToString())
                .SendAsync(NotificationHub.ClientMethod, message, ct);
        }
        catch (Exception ex)
        {
            // Best-effort: the reply/vote that triggered this already succeeded,
            // so a push failure must never surface to the caller.
            logger.LogWarning(ex, "Failed to push {Kind} notification to user {UserId}.",
                message.Kind, recipientUserId);
        }
    }

    private static string VoteWord(int value) => value > 0 ? "an upvote" : "a downvote";
}

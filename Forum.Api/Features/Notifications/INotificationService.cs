// Forum.Api/Features/Notifications/INotificationService.cs
namespace Forum.Api.Features.Notifications;

/// <summary>
/// Pushes real-time notifications to a single user. All sends are best-effort:
/// a delivery failure is logged and swallowed so it can never fail the write
/// (reply/vote) that triggered it. Callers decide *whether* to notify (e.g. skip
/// self-replies and self-votes); this service only formats and delivers.
/// </summary>
public interface INotificationService
{
    /// <summary>Tells a thread author that someone replied to their thread.</summary>
    Task NotifyThreadRepliedAsync(
        int recipientUserId, string replierUserName, int threadId, string threadTitle,
        CancellationToken ct = default);

    /// <summary>Tells a thread author their thread received a vote (value +1 or -1).</summary>
    Task NotifyThreadVotedAsync(
        int recipientUserId, int value, int threadId, string threadTitle,
        CancellationToken ct = default);

    /// <summary>Tells a reply author their reply received a vote (value +1 or -1).</summary>
    Task NotifyPostVotedAsync(
        int recipientUserId, int value, int threadId, string threadTitle,
        CancellationToken ct = default);
}

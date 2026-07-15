// Forum.Api/Features/Notifications/NotificationDtos.cs
namespace Forum.Api.Features.Notifications;

/// <summary>
/// Payload pushed to a connected client over the notification hub.
/// Kind is a stable machine tag ("reply" | "thread-vote" | "post-vote");
/// Text is ready-to-display; Url is the in-app destination for a click.
/// </summary>
public record NotificationMessage(string Kind, string Text, string Url, DateTime CreatedAtUtc);

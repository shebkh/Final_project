// Forum.Web/Features/Notifications/NotificationModels.cs
namespace Forum.Web.Features.Notifications;

/// <summary>
/// Mirror of the API's hub payload (Forum.Api NotificationMessage). Kind is a
/// stable machine tag ("reply" | "thread-vote" | "post-vote"); Text is
/// ready-to-display; Url is the in-app destination for a click.
/// </summary>
public sealed record NotificationMessage(string Kind, string Text, string Url, DateTime CreatedAtUtc);

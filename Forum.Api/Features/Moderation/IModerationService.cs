// Forum.Api/Features/Moderation/IModerationService.cs
namespace Forum.Api.Features.Moderation;

/// <summary>
/// Outcome classification for moderation operations. Authorization (moderator-only)
/// is enforced at the controller via [Authorize(Roles = ...)], so the only domain
/// failure the service expresses is a missing target thread.
/// </summary>
public enum ModerationError
{
    None = 0,
    ThreadNotFound
}

/// <summary>Generic result carrying an optional payload plus an error classification.</summary>
public readonly record struct ModerationResult<T>(T? Value, ModerationError Error) where T : class
{
    public bool Succeeded => Error == ModerationError.None;

    public static ModerationResult<T> Success(T value) => new(value, ModerationError.None);
    public static ModerationResult<T> Fail(ModerationError error) => new(null, error);
}

public interface IModerationService
{
    Task<ModerationResult<ThreadModerationResponse>> SetPinnedAsync(
        int threadId, bool pinned, CancellationToken ct = default);

    Task<ModerationResult<ThreadModerationResponse>> SetLockedAsync(
        int threadId, bool locked, CancellationToken ct = default);
}

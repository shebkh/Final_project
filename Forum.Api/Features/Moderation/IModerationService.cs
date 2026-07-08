// Forum.Api/Features/Moderation/IModerationService.cs
namespace Forum.Api.Features.Moderation;

/// <summary>
/// Outcome classification for moderation operations. Authorization (moderator-only)
/// is enforced at the controller via [Authorize(Roles = ...)], so the domain failures
/// the service expresses are a missing target thread or, for moves, a missing category.
/// </summary>
public enum ModerationError
{
    None = 0,
    ThreadNotFound,

    /// <summary>The move target CategoryId does not reference an existing category.</summary>
    CategoryNotFound
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

    /// <summary>Files the thread under the given category (null = uncategorize).</summary>
    Task<ModerationResult<ThreadModerationResponse>> MoveAsync(
        int threadId, int? categoryId, CancellationToken ct = default);
}

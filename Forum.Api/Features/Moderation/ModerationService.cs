// Forum.Api/Features/Moderation/ModerationService.cs
namespace Forum.Api.Features.Moderation;

/// <summary>
/// Moderator-only thread actions: pin/unpin and lock/unlock. Deleting other users'
/// content is handled by the Threads/Posts services (which accept an isModerator flag),
/// so this service owns only the thread-flag toggles.
/// </summary>
public sealed class ModerationService(IModerationRepository repository) : IModerationService
{
    public Task<ModerationResult<ThreadModerationResponse>> SetPinnedAsync(
        int threadId, bool pinned, CancellationToken ct = default) =>
        ApplyAsync(threadId, t => t.IsPinned = pinned, ct);

    public Task<ModerationResult<ThreadModerationResponse>> SetLockedAsync(
        int threadId, bool locked, CancellationToken ct = default) =>
        ApplyAsync(threadId, t => t.IsLocked = locked, ct);

    private async Task<ModerationResult<ThreadModerationResponse>> ApplyAsync(
        int threadId, Action<Threads.ForumThread> mutate, CancellationToken ct)
    {
        // Tracked, no-Include fetch: mutating + SaveChanges writes only the Threads row.
        var thread = await repository.GetThreadForUpdateAsync(threadId, ct);
        if (thread is null)
            return ModerationResult<ThreadModerationResponse>.Fail(ModerationError.ThreadNotFound);

        mutate(thread);
        await repository.SaveChangesAsync(ct);

        return ModerationResult<ThreadModerationResponse>.Success(
            new ThreadModerationResponse(thread.Id, thread.IsPinned, thread.IsLocked));
    }
}

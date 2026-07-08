// Forum.Api/Features/Moderation/ModerationService.cs
using Forum.Api.Features.Categories;

namespace Forum.Api.Features.Moderation;

/// <summary>
/// Moderator-only thread actions: pin/unpin, lock/unlock, and move between
/// categories. Deleting other users' content is handled by the Threads/Posts
/// services (which accept an isModerator flag), so this service owns only the
/// thread-flag toggles and the category move.
/// </summary>
public sealed class ModerationService(
    IModerationRepository repository,
    ICategoryRepository categoryRepository) : IModerationService
{
    public Task<ModerationResult<ThreadModerationResponse>> SetPinnedAsync(
        int threadId, bool pinned, CancellationToken ct = default) =>
        ApplyAsync(threadId, t => t.IsPinned = pinned, ct);

    public Task<ModerationResult<ThreadModerationResponse>> SetLockedAsync(
        int threadId, bool locked, CancellationToken ct = default) =>
        ApplyAsync(threadId, t => t.IsLocked = locked, ct);

    public async Task<ModerationResult<ThreadModerationResponse>> MoveAsync(
        int threadId, int? categoryId, CancellationToken ct = default)
    {
        // Null uncategorizes; otherwise the target category must exist.
        if (categoryId is not null && !await categoryRepository.ExistsAsync(categoryId.Value, ct))
            return ModerationResult<ThreadModerationResponse>.Fail(ModerationError.CategoryNotFound);

        return await ApplyAsync(threadId, t => t.CategoryId = categoryId, ct);
    }

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
            new ThreadModerationResponse(thread.Id, thread.IsPinned, thread.IsLocked, thread.CategoryId));
    }
}

// Forum.Api/Features/Posts/IPostRepository.cs
namespace Forum.Api.Features.Posts;

public interface IPostRepository
{
    /// <summary>True if the parent thread exists (used to 404 on create).</summary>
    Task<bool> ThreadExistsAsync(int threadId, CancellationToken ct = default);

    /// <summary>
    /// The lock state of a thread: (exists, isLocked). exists is false when the thread is
    /// missing. Used to reject new replies / reply edits on a moderator-locked thread.
    /// </summary>
    Task<(bool Exists, bool IsLocked)> GetThreadLockStateAsync(int threadId, CancellationToken ct = default);

    /// <summary>Oldest-first page of replies for a thread, with Author loaded (read-only).</summary>
    Task<IReadOnlyList<Post>> ListByThreadAsync(int threadId, int skip, int take, CancellationToken ct = default);

    /// <summary>Total reply count for a thread, for paging metadata.</summary>
    Task<int> CountByThreadAsync(int threadId, CancellationToken ct = default);

    /// <summary>Single post with its Author loaded, read-only (no tracking). Null if not found.</summary>
    Task<Post?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Tracked fetch WITHOUT the Author/Thread navigations, for the write path.
    /// Mutating + SaveChangesAsync emits a minimal UPDATE confined to the Posts row.
    /// </summary>
    Task<Post?> GetForUpdateAsync(int id, CancellationToken ct = default);

    Task<Post> AddAsync(Post post, CancellationToken ct = default);

    /// <summary>Persists pending changes to a tracked entity returned by GetForUpdateAsync.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Deletes a tracked entity returned by GetForUpdateAsync.</summary>
    Task DeleteAsync(Post post, CancellationToken ct = default);
}

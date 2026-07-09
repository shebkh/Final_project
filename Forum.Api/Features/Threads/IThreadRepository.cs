// Forum.Api/Features/Threads/IThreadRepository.cs
namespace Forum.Api.Features.Threads;

public interface IThreadRepository
{
    /// <summary>
    /// Newest-first page of threads, with the Author, Category, and Tag navigations
    /// loaded (read-only). A categoryId filter matches the category or its direct
    /// children; a tag filter matches threads carrying that normalized tag name.
    /// </summary>
    Task<IReadOnlyList<ForumThread>> ListAsync(
        int skip, int take, int? categoryId = null, string? tag = null, CancellationToken ct = default);

    /// <summary>Total thread count under the same optional filters, for paging metadata.</summary>
    Task<int> CountAsync(int? categoryId = null, string? tag = null, CancellationToken ct = default);

    /// <summary>Single thread with Author, Category, and Tags loaded, read-only (no tracking). Null if not found.</summary>
    Task<ForumThread?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Tracked fetch of a thread WITHOUT the Author navigation, for the write path.
    /// Mutating the returned entity and calling SaveChangesAsync emits a minimal,
    /// correct UPDATE confined to the Threads row. Null if not found.
    /// </summary>
    Task<ForumThread?> GetForUpdateAsync(int id, CancellationToken ct = default);

    Task<ForumThread> AddAsync(ForumThread thread, CancellationToken ct = default);

    /// <summary>Persists pending changes to a tracked entity returned by GetForUpdateAsync.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Deletes a tracked entity returned by GetForUpdateAsync.</summary>
    Task DeleteAsync(ForumThread thread, CancellationToken ct = default);

    /// <summary>
    /// Replaces the thread's tag set with the given normalized names, creating
    /// missing Tag rows (unique-violation-safe on concurrent creates).
    /// </summary>
    Task SetTagsAsync(int threadId, IReadOnlyList<string> names, CancellationToken ct = default);
}

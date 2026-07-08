// Forum.Api/Features/Categories/ICategoryRepository.cs
namespace Forum.Api.Features.Categories;

public interface ICategoryRepository
{
    /// <summary>All categories ordered by name, read-only (no tracking).</summary>
    Task<IReadOnlyList<Category>> ListAsync(CancellationToken ct = default);

    /// <summary>Per-category count of threads assigned directly to each category.</summary>
    Task<IReadOnlyDictionary<int, int>> ThreadCountsAsync(CancellationToken ct = default);

    /// <summary>Single category, read-only (no tracking). Null if not found.</summary>
    Task<Category?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Tracked fetch WITHOUT navigations, for the write path. Mutating the returned
    /// entity and calling SaveChangesAsync emits a minimal UPDATE confined to the
    /// Categories row. Null if not found.
    /// </summary>
    Task<Category?> GetForUpdateAsync(int id, CancellationToken ct = default);

    /// <summary>True if a category with this id exists. Used by other slices to validate FKs.</summary>
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);

    /// <summary>True if any category has this one as its parent.</summary>
    Task<bool> HasChildrenAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Inserts the category; returns false (without throwing) when the unique slug
    /// index rejects it — SQL 2627/2601 — so the caller can report a name conflict.
    /// </summary>
    Task<bool> TryAddAsync(Category category, CancellationToken ct = default);

    /// <summary>
    /// Persists pending changes to a tracked entity returned by GetForUpdateAsync;
    /// returns false when the unique slug index rejects the change (name conflict).
    /// </summary>
    Task<bool> TrySaveChangesAsync(CancellationToken ct = default);

    /// <summary>Deletes a tracked entity returned by GetForUpdateAsync.</summary>
    Task DeleteAsync(Category category, CancellationToken ct = default);
}

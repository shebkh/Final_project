// Forum.Api/Features/Categories/ICategoryService.cs
namespace Forum.Api.Features.Categories;

/// <summary>
/// Outcome classification for category operations. Keeps HTTP-status decisions
/// in the controller while the service expresses domain results.
/// </summary>
public enum CategoryError
{
    None = 0,
    NotFound,

    /// <summary>The requested ParentId does not reference an existing category.</summary>
    ParentNotFound,

    /// <summary>The requested parent is itself a sub-category (only one level of nesting).</summary>
    ParentIsChild,

    /// <summary>The category has sub-categories and cannot be deleted or re-parented.</summary>
    HasChildren,

    /// <summary>Another category already uses this name (unique slug violation).</summary>
    NameTaken
}

/// <summary>Generic result carrying an optional payload plus an error classification.</summary>
public readonly record struct CategoryResult<T>(T? Value, CategoryError Error) where T : class
{
    public bool Succeeded => Error == CategoryError.None;

    public static CategoryResult<T> Success(T value) => new(value, CategoryError.None);
    public static CategoryResult<T> Fail(CategoryError error) => new(null, error);
}

public interface ICategoryService
{
    /// <summary>All categories (roots and children flat), with per-category thread counts.</summary>
    Task<IReadOnlyList<CategoryResponse>> ListAsync(CancellationToken ct = default);

    Task<CategoryResult<CategoryResponse>> GetByIdAsync(int id, CancellationToken ct = default);

    Task<CategoryResult<CategoryResponse>> CreateAsync(
        CreateCategoryRequest request, CancellationToken ct = default);

    Task<CategoryResult<CategoryResponse>> UpdateAsync(
        int id, UpdateCategoryRequest request, CancellationToken ct = default);

    Task<CategoryResult<CategoryResponse>> DeleteAsync(int id, CancellationToken ct = default);
}

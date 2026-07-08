// Forum.Api/Features/Categories/CategoryService.cs
using System.Text;

namespace Forum.Api.Features.Categories;

public sealed class CategoryService(ICategoryRepository repository) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryResponse>> ListAsync(CancellationToken ct = default)
    {
        var categories = await repository.ListAsync(ct);
        var counts = await repository.ThreadCountsAsync(ct);
        return categories
            .Select(c => ToResponse(c, counts.GetValueOrDefault(c.Id)))
            .ToList();
    }

    public async Task<CategoryResult<CategoryResponse>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var category = await repository.GetByIdAsync(id, ct);
        if (category is null)
            return CategoryResult<CategoryResponse>.Fail(CategoryError.NotFound);

        var counts = await repository.ThreadCountsAsync(ct);
        return CategoryResult<CategoryResponse>.Success(ToResponse(category, counts.GetValueOrDefault(id)));
    }

    public async Task<CategoryResult<CategoryResponse>> CreateAsync(
        CreateCategoryRequest request, CancellationToken ct = default)
    {
        var parentError = await ValidateParentAsync(request.ParentId, id: null, ct);
        if (parentError != CategoryError.None)
            return CategoryResult<CategoryResponse>.Fail(parentError);

        var name = request.Name.Trim();
        var category = new Category
        {
            Name = name,
            Slug = Slugify(name),
            ParentId = request.ParentId,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Unique slug index doubles as the duplicate-name guard (SQL 2627/2601 → false).
        if (!await repository.TryAddAsync(category, ct))
            return CategoryResult<CategoryResponse>.Fail(CategoryError.NameTaken);

        return CategoryResult<CategoryResponse>.Success(ToResponse(category, threadCount: 0));
    }

    public async Task<CategoryResult<CategoryResponse>> UpdateAsync(
        int id, UpdateCategoryRequest request, CancellationToken ct = default)
    {
        // Tracked, no-Include fetch: mutating + SaveChanges writes only the Categories row.
        var category = await repository.GetForUpdateAsync(id, ct);
        if (category is null)
            return CategoryResult<CategoryResponse>.Fail(CategoryError.NotFound);

        var parentError = await ValidateParentAsync(request.ParentId, id, ct);
        if (parentError != CategoryError.None)
            return CategoryResult<CategoryResponse>.Fail(parentError);

        // A category that has children must stay a root (single level of nesting).
        if (request.ParentId is not null && await repository.HasChildrenAsync(id, ct))
            return CategoryResult<CategoryResponse>.Fail(CategoryError.HasChildren);

        var name = request.Name.Trim();
        category.Name = name;
        category.Slug = Slugify(name);
        category.ParentId = request.ParentId;

        if (!await repository.TrySaveChangesAsync(ct))
            return CategoryResult<CategoryResponse>.Fail(CategoryError.NameTaken);

        var counts = await repository.ThreadCountsAsync(ct);
        return CategoryResult<CategoryResponse>.Success(ToResponse(category, counts.GetValueOrDefault(id)));
    }

    public async Task<CategoryResult<CategoryResponse>> DeleteAsync(int id, CancellationToken ct = default)
    {
        var category = await repository.GetForUpdateAsync(id, ct);
        if (category is null)
            return CategoryResult<CategoryResponse>.Fail(CategoryError.NotFound);

        // The self-FK is Restrict; sub-categories must be deleted or re-parented first.
        if (await repository.HasChildrenAsync(id, ct))
            return CategoryResult<CategoryResponse>.Fail(CategoryError.HasChildren);

        var snapshot = ToResponse(category, threadCount: 0);

        // Threads referencing this category are uncategorized by the SetNull FK.
        await repository.DeleteAsync(category, ct);
        return CategoryResult<CategoryResponse>.Success(snapshot);
    }

    /// <summary>
    /// Validates a requested ParentId: it must exist, must itself be a root category
    /// (one level of nesting only), and must not be the category being edited.
    /// </summary>
    private async Task<CategoryError> ValidateParentAsync(int? parentId, int? id, CancellationToken ct)
    {
        if (parentId is null)
            return CategoryError.None;

        if (parentId == id)
            return CategoryError.ParentIsChild;

        var parent = await repository.GetByIdAsync(parentId.Value, ct);
        if (parent is null)
            return CategoryError.ParentNotFound;

        return parent.ParentId is null ? CategoryError.None : CategoryError.ParentIsChild;
    }

    /// <summary>Lowercase, alphanumerics kept, everything else collapsed to single hyphens.</summary>
    private static string Slugify(string name)
    {
        var sb = new StringBuilder(name.Length);
        var lastWasHyphen = true; // suppress leading hyphens

        foreach (var ch in name.ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(ch))
            {
                sb.Append(ch);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen)
            {
                sb.Append('-');
                lastWasHyphen = true;
            }
        }

        // Trim a trailing hyphen; fall back for names with no ASCII alphanumerics.
        var slug = sb.ToString().TrimEnd('-');
        return slug.Length > 0 ? slug : "category";
    }

    private static CategoryResponse ToResponse(Category c, int threadCount) =>
        new(c.Id, c.Name, c.Slug, c.ParentId, threadCount);
}

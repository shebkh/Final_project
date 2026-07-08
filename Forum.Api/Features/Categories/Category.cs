// Forum.Api/Features/Categories/Category.cs
namespace Forum.Api.Features.Categories;

/// <summary>
/// EF Core entity for a discussion category. Categories form a single-level
/// hierarchy: a root category may have sub-categories (ParentId set), but a
/// sub-category can never have children of its own — the service enforces this.
/// Never exposed directly over the API — always projected to a DTO.
/// </summary>
public class Category
{
    public int Id { get; set; }

    public required string Name { get; set; }

    /// <summary>
    /// URL-friendly identifier generated from Name (lowercase, hyphenated).
    /// Unique across all categories; uniqueness doubles as the duplicate-name guard.
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>FK to the parent category; null for root categories.</summary>
    public int? ParentId { get; set; }

    /// <summary>Navigation to the parent. Loaded explicitly when needed.</summary>
    public Category? Parent { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

// Forum.Api/Features/Categories/CategoryDtos.cs
namespace Forum.Api.Features.Categories;

/// <summary>Request body for POST /api/categories.</summary>
public record CreateCategoryRequest(string Name, int? ParentId);

/// <summary>Request body for PUT /api/categories/{id}.</summary>
public record UpdateCategoryRequest(string Name, int? ParentId);

/// <summary>
/// Projection for category listings and detail. ThreadCount counts threads
/// assigned directly to this category (not aggregated from children).
/// </summary>
public record CategoryResponse(
    int Id,
    string Name,
    string Slug,
    int? ParentId,
    int ThreadCount);

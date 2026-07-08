// Forum.Web/Features/Categories/CategoryModels.cs
using System.ComponentModel.DataAnnotations;

namespace Forum.Web.Features.Categories;

// --- Form model (bound by EditForm; the API re-validates with FluentValidation) ---

public sealed class CategoryEditModel
{
    [Required, StringLength(80, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    public int? ParentId { get; set; }
}

// --- Wire DTOs matching the API's request/response shapes ---

public record CreateCategoryRequest(string Name, int? ParentId);
public record UpdateCategoryRequest(string Name, int? ParentId);

public record CategoryResponse(
    int Id,
    string Name,
    string Slug,
    int? ParentId,
    int ThreadCount);

/// <summary>Result wrapper so components handle failures without exceptions.</summary>
public record CategoryOutcome<T>(bool Succeeded, T? Data, string? Error) where T : class
{
    public static CategoryOutcome<T> Ok(T data) => new(true, data, null);
    public static CategoryOutcome<T> Failed(string error) => new(false, null, error);
}

/// <summary>Result wrapper for operations with no payload (e.g. delete).</summary>
public record CategoryActionOutcome(bool Succeeded, string? Error)
{
    public static CategoryActionOutcome Ok() => new(true, null);
    public static CategoryActionOutcome Failed(string error) => new(false, error);
}

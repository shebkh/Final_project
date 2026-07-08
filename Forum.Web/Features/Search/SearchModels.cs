// Forum.Web/Features/Search/SearchModels.cs
namespace Forum.Web.Features.Search;

// --- Wire DTOs matching the API's response shapes ---

public record SearchResultResponse(
    string Kind,
    int ThreadId,
    string ThreadTitle,
    int? PostId,
    string Snippet,
    int AuthorId,
    string AuthorUserName,
    DateTime CreatedAtUtc,
    int? CategoryId,
    string? CategoryName);

public record PagedSearchResponse(IReadOnlyList<SearchResultResponse> Items, int Total);

/// <summary>Result wrapper so components handle failures without exceptions.</summary>
public record SearchOutcome<T>(bool Succeeded, T? Data, string? Error) where T : class
{
    public static SearchOutcome<T> Ok(T data) => new(true, data, null);
    public static SearchOutcome<T> Failed(string error) => new(false, null, error);
}

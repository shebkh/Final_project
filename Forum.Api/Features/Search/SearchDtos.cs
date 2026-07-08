// Forum.Api/Features/Search/SearchDtos.cs
namespace Forum.Api.Features.Search;

/// <summary>
/// One search hit. Kind is "Thread" (matched a thread's title/body) or "Reply"
/// (matched a post's body — PostId set, links to its parent thread).
/// </summary>
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

/// <summary>Items plus the unpaged total, returned by the service in a single call.</summary>
public record PagedSearchResponse(IReadOnlyList<SearchResultResponse> Items, int Total);

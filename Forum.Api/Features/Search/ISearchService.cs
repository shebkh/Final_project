// Forum.Api/Features/Search/ISearchService.cs
namespace Forum.Api.Features.Search;

/// <summary>
/// Outcome classification for search operations. Keeps HTTP-status decisions
/// in the controller while the service expresses domain results.
/// </summary>
public enum SearchError
{
    None = 0,

    /// <summary>The search term is under two characters after trimming.</summary>
    QueryTooShort
}

/// <summary>Generic result carrying an optional payload plus an error classification.</summary>
public readonly record struct SearchResult<T>(T? Value, SearchError Error) where T : class
{
    public bool Succeeded => Error == SearchError.None;

    public static SearchResult<T> Success(T value) => new(value, SearchError.None);
    public static SearchResult<T> Fail(SearchError error) => new(null, error);
}

public interface ISearchService
{
    /// <summary>
    /// Keyword search across thread titles/bodies and reply bodies, newest first.
    /// An optional category filter matches the category or its direct children.
    /// </summary>
    Task<SearchResult<PagedSearchResponse>> SearchAsync(
        string? q, int? categoryId, int page, int pageSize, CancellationToken ct = default);
}

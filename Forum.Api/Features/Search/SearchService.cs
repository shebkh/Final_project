// Forum.Api/Features/Search/SearchService.cs
namespace Forum.Api.Features.Search;

public sealed class SearchService(ISearchRepository repository) : ISearchService
{
    private const int MinQueryLength = 2;
    private const int MaxPageSize = 50;
    private const int SnippetLength = 200;

    public async Task<SearchResult<PagedSearchResponse>> SearchAsync(
        string? q, int? categoryId, int page, int pageSize, CancellationToken ct = default)
    {
        var term = q?.Trim() ?? string.Empty;
        if (term.Length < MinQueryLength)
            return SearchResult<PagedSearchResponse>.Fail(SearchError.QueryTooShort);

        var (skip, take) = Normalize(page, pageSize);
        var (hits, total) = await repository.SearchAsync(term, categoryId, skip, take, ct);

        var items = hits
            .Select(h => new SearchResultResponse(
                h.Kind,
                h.ThreadId,
                h.ThreadTitle,
                h.PostId,
                BuildSnippet(h.Body, term),
                h.AuthorId,
                h.AuthorUserName,
                h.CreatedAtUtc,
                h.CategoryId,
                h.CategoryName))
            .ToList();

        return SearchResult<PagedSearchResponse>.Success(new PagedSearchResponse(items, total));
    }

    private static (int skip, int take) Normalize(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;
        return ((page - 1) * pageSize, pageSize);
    }

    /// <summary>
    /// A ~200-char window centered on the first occurrence of the term. Threads
    /// that matched only on their title have no body occurrence — those fall
    /// back to a from-the-start excerpt (the ThreadService excerpt rule).
    /// </summary>
    private static string BuildSnippet(string body, string term)
    {
        var index = body.IndexOf(term, StringComparison.OrdinalIgnoreCase);
        if (index < 0 || body.Length <= SnippetLength)
        {
            return body.Length <= SnippetLength
                ? body
                : body[..SnippetLength].TrimEnd() + "…";
        }

        // Center the window on the match, clamped to the body's bounds.
        var start = Math.Clamp(index - (SnippetLength - term.Length) / 2, 0, body.Length - SnippetLength);
        var snippet = body.Substring(start, SnippetLength).Trim();

        if (start > 0) snippet = "…" + snippet;
        if (start + SnippetLength < body.Length) snippet += "…";
        return snippet;
    }
}

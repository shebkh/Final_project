// Forum.Api/Features/Search/ISearchRepository.cs
namespace Forum.Api.Features.Search;

/// <summary>
/// Common projection for a thread or reply hit. Carries the FULL body — the
/// service cuts a match-centered snippet from it; never leaves the API layer.
/// </summary>
public record SearchHit(
    string Kind,
    int ThreadId,
    string ThreadTitle,
    int? PostId,
    string Body,
    int AuthorId,
    string AuthorUserName,
    DateTime CreatedAtUtc,
    int? CategoryId,
    string? CategoryName);

public interface ISearchRepository
{
    /// <summary>
    /// One page of combined thread + reply hits (newest first) plus the unpaged
    /// total, in a single repository call. The term is matched with LIKE against
    /// thread titles/bodies and reply bodies; wildcards in the term are escaped.
    /// </summary>
    Task<(IReadOnlyList<SearchHit> Hits, int Total)> SearchAsync(
        string term, int? categoryId, int skip, int take, CancellationToken ct = default);
}

// Forum.Api/Features/Search/SearchRepository.cs
using Forum.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Forum.Api.Features.Search;

/// <summary>
/// Read-only search over threads and replies. Queries other slices' DbSets via
/// AppDbContext (same precedent as ProfileRepository). LIKE is used instead of
/// SQL Server Full-Text Search because FTS is unavailable on LocalDB.
/// </summary>
public sealed class SearchRepository(AppDbContext db) : ISearchRepository
{
    public async Task<(IReadOnlyList<SearchHit> Hits, int Total)> SearchAsync(
        string term, int? categoryId, int skip, int take, CancellationToken ct = default)
    {
        var pattern = "%" + EscapeLike(term) + "%";

        // Both sides project to the SAME anonymous shape: EF only translates
        // Concat (UNION ALL) over server-side projections — a record constructor
        // here counts as a client projection and throws at translation time.
        var threadHits = db.Threads
            .AsNoTracking()
            .Where(t => EF.Functions.Like(t.Title, pattern) || EF.Functions.Like(t.Body, pattern))
            .Where(t => categoryId == null
                || t.CategoryId == categoryId
                || (t.Category != null && t.Category.ParentId == categoryId))
            .Select(t => new
            {
                Kind = "Thread",
                ThreadId = t.Id,
                ThreadTitle = t.Title,
                PostId = (int?)null,
                t.Body,
                t.AuthorId,
                AuthorUserName = t.Author!.UserName,
                t.CreatedAtUtc,
                t.CategoryId,
                CategoryName = t.Category != null ? t.Category.Name : null
            });

        var replyHits = db.Posts
            .AsNoTracking()
            .Where(p => EF.Functions.Like(p.Body, pattern))
            .Where(p => categoryId == null
                || p.Thread!.CategoryId == categoryId
                || (p.Thread!.Category != null && p.Thread.Category.ParentId == categoryId))
            .Select(p => new
            {
                Kind = "Reply",
                p.ThreadId,
                ThreadTitle = p.Thread!.Title,
                PostId = (int?)p.Id,
                p.Body,
                p.AuthorId,
                AuthorUserName = p.Author!.UserName,
                p.CreatedAtUtc,
                CategoryId = p.Thread!.CategoryId,
                CategoryName = p.Thread!.Category != null ? p.Thread.Category.Name : null
            });

        // UNION ALL in SQL, so ordering/paging/count run server-side over the
        // combined set — one page fetch plus one count.
        var combined = threadHits.Concat(replyHits);

        var total = await combined.CountAsync(ct);
        var page = await combined
            .OrderByDescending(h => h.CreatedAtUtc)
            .ThenByDescending(h => h.ThreadId)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        var hits = page
            .Select(h => new SearchHit(
                h.Kind, h.ThreadId, h.ThreadTitle, h.PostId, h.Body,
                h.AuthorId, h.AuthorUserName, h.CreatedAtUtc, h.CategoryId, h.CategoryName))
            .ToList();

        return (hits, total);
    }

    // SQL Server LIKE: '[' opens a character class; '%' and '_' are wildcards.
    // Bracket-escaping each makes user input match literally ("50%" ≠ match-all).
    private static string EscapeLike(string term) =>
        term.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
}

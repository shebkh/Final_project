// Forum.Api/Features/Posts/PostRepository.cs
using Forum.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Forum.Api.Features.Posts;

public sealed class PostRepository(AppDbContext db) : IPostRepository
{
    public Task<bool> ThreadExistsAsync(int threadId, CancellationToken ct = default) =>
        db.Threads.AnyAsync(t => t.Id == threadId, ct);

    public async Task<(bool Exists, bool IsLocked)> GetThreadLockStateAsync(
        int threadId, CancellationToken ct = default)
    {
        var row = await db.Threads
            .AsNoTracking()
            .Where(t => t.Id == threadId)
            .Select(t => new { t.IsLocked })
            .FirstOrDefaultAsync(ct);

        return row is null ? (false, false) : (true, row.IsLocked);
    }

    public async Task<ThreadInfo?> GetThreadInfoAsync(int threadId, CancellationToken ct = default)
    {
        var row = await db.Threads
            .AsNoTracking()
            .Where(t => t.Id == threadId)
            .Select(t => new { t.AuthorId, t.Title, t.IsLocked })
            .FirstOrDefaultAsync(ct);

        return row is null ? null : new ThreadInfo(row.AuthorId, row.Title, row.IsLocked);
    }

    public async Task<IReadOnlyList<Post>> ListByThreadAsync(
        int threadId, int skip, int take, CancellationToken ct = default) =>
        await db.Posts
            .AsNoTracking()
            .Include(p => p.Author)
            .Where(p => p.ThreadId == threadId)
            .OrderBy(p => p.CreatedAtUtc)
            .ThenBy(p => p.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public Task<int> CountByThreadAsync(int threadId, CancellationToken ct = default) =>
        db.Posts.CountAsync(p => p.ThreadId == threadId, ct);

    public Task<Post?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Posts
            .AsNoTracking()
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    // Tracked, no Include — so SaveChanges writes only the Posts row and never
    // traverses into (and re-persists) the Author/User or Thread aggregates.
    public Task<Post?> GetForUpdateAsync(int id, CancellationToken ct = default) =>
        db.Posts.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Post> AddAsync(Post post, CancellationToken ct = default)
    {
        db.Posts.Add(post);
        await db.SaveChangesAsync(ct);
        return post;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);

    public async Task DeleteAsync(Post post, CancellationToken ct = default)
    {
        db.Posts.Remove(post);
        await db.SaveChangesAsync(ct);
    }
}

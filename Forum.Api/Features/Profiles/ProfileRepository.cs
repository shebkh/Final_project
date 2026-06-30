// Forum.Api/Features/Profiles/ProfileRepository.cs
using Forum.Api.Data;
using Forum.Api.Features.Auth;
using Forum.Api.Features.Posts;
using Forum.Api.Features.Threads;
using Microsoft.EntityFrameworkCore;

namespace Forum.Api.Features.Profiles;

public sealed class ProfileRepository(AppDbContext db) : IProfileRepository
{
    public Task<bool> UserExistsAsync(int userId, CancellationToken ct = default) =>
        db.Users.AnyAsync(u => u.Id == userId, ct);

    public Task<User?> GetUserAsync(int userId, CancellationToken ct = default) =>
        db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

    public Task<int> CountThreadsByAuthorAsync(int userId, CancellationToken ct = default) =>
        db.Threads.CountAsync(t => t.AuthorId == userId, ct);

    public Task<int> CountPostsByAuthorAsync(int userId, CancellationToken ct = default) =>
        db.Posts.CountAsync(p => p.AuthorId == userId, ct);

    public async Task<int> GetReputationAsync(int userId, CancellationToken ct = default)
    {
        // Votes cast on threads this user authored. Sum over an empty set is NULL in SQL,
        // so project to int? and coalesce to 0.
        var threadRep = await db.ThreadVotes
            .Where(v => db.Threads.Any(t => t.Id == v.ThreadId && t.AuthorId == userId))
            .SumAsync(v => (int?)v.Value, ct) ?? 0;

        // Votes cast on posts this user authored.
        var postRep = await db.PostVotes
            .Where(v => db.Posts.Any(p => p.Id == v.PostId && p.AuthorId == userId))
            .SumAsync(v => (int?)v.Value, ct) ?? 0;

        return threadRep + postRep;
    }

    public async Task<IReadOnlyList<ForumThread>> ListThreadsByAuthorAsync(
        int userId, int skip, int take, CancellationToken ct = default) =>
        await db.Threads
            .AsNoTracking()
            .Where(t => t.AuthorId == userId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ThenByDescending(t => t.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Post>> ListPostsByAuthorAsync(
        int userId, int skip, int take, CancellationToken ct = default) =>
        await db.Posts
            .AsNoTracking()
            .Include(p => p.Thread)
            .Where(p => p.AuthorId == userId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ThenByDescending(p => p.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
}

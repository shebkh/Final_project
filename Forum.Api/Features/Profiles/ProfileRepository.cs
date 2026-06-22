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

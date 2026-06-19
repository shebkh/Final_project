// Forum.Api/Features/Threads/ThreadRepository.cs
using Forum.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Forum.Api.Features.Threads;

public sealed class ThreadRepository(AppDbContext db) : IThreadRepository
{
    public async Task<IReadOnlyList<ForumThread>> ListAsync(int skip, int take, CancellationToken ct = default) =>
        await db.Threads
            .AsNoTracking()
            .Include(t => t.Author)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ThenByDescending(t => t.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public Task<int> CountAsync(CancellationToken ct = default) =>
        db.Threads.CountAsync(ct);

    public Task<ForumThread?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Threads
            .AsNoTracking()
            .Include(t => t.Author)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    // Tracked, no Include — so SaveChanges writes only the Threads row and never
    // traverses into (and re-persists) the Author/User aggregate.
    public Task<ForumThread?> GetForUpdateAsync(int id, CancellationToken ct = default) =>
        db.Threads.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<ForumThread> AddAsync(ForumThread thread, CancellationToken ct = default)
    {
        db.Threads.Add(thread);
        await db.SaveChangesAsync(ct);
        return thread;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);

    public async Task DeleteAsync(ForumThread thread, CancellationToken ct = default)
    {
        db.Threads.Remove(thread);
        await db.SaveChangesAsync(ct);
    }
}

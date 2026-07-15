// Forum.Api/Features/Threads/ThreadRepository.cs
using Forum.Api.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Forum.Api.Features.Threads;

public sealed class ThreadRepository(AppDbContext db) : IThreadRepository
{
    public async Task<IReadOnlyList<ForumThread>> ListAsync(
        int skip, int take, int? categoryId = null, string? tag = null, CancellationToken ct = default) =>
        await Filter(db.Threads.AsNoTracking(), categoryId, tag)
            .Include(t => t.Author)
            .Include(t => t.Category)
            .Include(t => t.ThreadTags).ThenInclude(tt => tt.Tag)
            .OrderByDescending(t => t.IsPinned)   // pinned threads first
            .ThenByDescending(t => t.CreatedAtUtc)
            .ThenByDescending(t => t.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public Task<int> CountAsync(int? categoryId = null, string? tag = null, CancellationToken ct = default) =>
        Filter(db.Threads, categoryId, tag).CountAsync(ct);

    public Task<ForumThread?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Threads
            .AsNoTracking()
            .Include(t => t.Author)
            .Include(t => t.Category)
            .Include(t => t.ThreadTags).ThenInclude(tt => tt.Tag)
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
        // Posts and votes reference the thread with Restrict FKs, so dependents must
        // go first — PostVotes → Posts → ThreadVotes — inside one transaction so a
        // failure can't leave the thread stripped of its replies but still alive.
        // (ThreadTags cascade with the thread row itself.)
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await db.PostVotes
            .Where(v => v.Post!.ThreadId == thread.Id)
            .ExecuteDeleteAsync(ct);
        await db.Posts
            .Where(p => p.ThreadId == thread.Id)
            .ExecuteDeleteAsync(ct);
        await db.ThreadVotes
            .Where(v => v.ThreadId == thread.Id)
            .ExecuteDeleteAsync(ct);

        db.Threads.Remove(thread);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A concurrent request already deleted the thread row, so the DELETE
            // affected 0 rows. The desired end-state — the thread is gone — is
            // already true, so treat it as success instead of a 500.
            db.Entry(thread).State = EntityState.Detached;
        }

        await tx.CommitAsync(ct);
    }

    public async Task SetTagsAsync(int threadId, IReadOnlyList<string> names, CancellationToken ct = default)
    {
        // Resolve each normalized name to a Tag id, creating missing tags with
        // the TryAdd pattern: a concurrent insert of the same name trips the
        // unique index (SQL 2627/2601) and we re-read the winner instead.
        var tagIds = new List<int>(names.Count);
        foreach (var name in names)
        {
            var existing = await db.Tags.AsNoTracking().FirstOrDefaultAsync(t => t.Name == name, ct);
            if (existing is not null)
            {
                tagIds.Add(existing.Id);
                continue;
            }

            var tag = new Tag { Name = name };
            db.Tags.Add(tag);
            try
            {
                await db.SaveChangesAsync(ct);
                tagIds.Add(tag.Id);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                db.Entry(tag).State = EntityState.Detached;
                var winner = await db.Tags.AsNoTracking().FirstAsync(t => t.Name == name, ct);
                tagIds.Add(winner.Id);
            }
        }

        // Replace the thread's join rows with the requested set (diff, not wipe,
        // so unchanged links produce no writes).
        var current = await db.ThreadTags.Where(tt => tt.ThreadId == threadId).ToListAsync(ct);
        db.ThreadTags.RemoveRange(current.Where(tt => !tagIds.Contains(tt.TagId)));
        var currentIds = current.Select(tt => tt.TagId).ToHashSet();
        db.ThreadTags.AddRange(tagIds
            .Where(id => !currentIds.Contains(id))
            .Select(id => new ThreadTag { ThreadId = threadId, TagId = id }));

        await db.SaveChangesAsync(ct);
    }

    // Filtering by a root category also surfaces threads filed under its direct
    // sub-categories, so browsing a root shows the whole family.
    private static IQueryable<ForumThread> Filter(IQueryable<ForumThread> query, int? categoryId, string? tag)
    {
        if (categoryId is not null)
            query = query.Where(t => t.CategoryId == categoryId
                || (t.Category != null && t.Category.ParentId == categoryId));

        if (!string.IsNullOrWhiteSpace(tag))
            query = query.Where(t => t.ThreadTags.Any(tt => tt.Tag!.Name == tag));

        return query;
    }

    // SQL Server: 2627 = unique constraint violation, 2601 = duplicate key in a unique index.
    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sql && (sql.Number == 2627 || sql.Number == 2601);
}

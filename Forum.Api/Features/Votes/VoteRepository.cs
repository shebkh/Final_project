// Forum.Api/Features/Votes/VoteRepository.cs
using Forum.Api.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Forum.Api.Features.Votes;

public sealed class VoteRepository(AppDbContext db) : IVoteRepository
{
    // --- Thread votes ---

    public Task<bool> ThreadExistsAsync(int threadId, CancellationToken ct = default) =>
        db.Threads.AnyAsync(t => t.Id == threadId, ct);

    // Tracked (no Include) so upsert mutates only the ThreadVotes row.
    public Task<ThreadVote?> GetThreadVoteAsync(int threadId, int userId, CancellationToken ct = default) =>
        db.ThreadVotes.FirstOrDefaultAsync(v => v.ThreadId == threadId && v.UserId == userId, ct);

    public async Task<bool> TryAddThreadVoteAsync(ThreadVote vote, CancellationToken ct = default)
    {
        db.ThreadVotes.Add(vote);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // A concurrent request inserted the (thread, user) vote first. Detach our
            // failed insert so the caller can re-read and update instead.
            db.Entry(vote).State = EntityState.Detached;
            return false;
        }
    }

    public async Task RemoveThreadVoteAsync(ThreadVote vote, CancellationToken ct = default)
    {
        db.ThreadVotes.Remove(vote);
        await db.SaveChangesAsync(ct);
    }

    public Task<VoteCounts> CountThreadVotesAsync(int threadId, CancellationToken ct = default) =>
        AggregateAsync(db.ThreadVotes.Where(v => v.ThreadId == threadId).Select(v => v.Value), ct);

    // --- Post votes ---

    public Task<bool> PostExistsAsync(int postId, CancellationToken ct = default) =>
        db.Posts.AnyAsync(p => p.Id == postId, ct);

    public Task<PostVote?> GetPostVoteAsync(int postId, int userId, CancellationToken ct = default) =>
        db.PostVotes.FirstOrDefaultAsync(v => v.PostId == postId && v.UserId == userId, ct);

    public async Task<bool> TryAddPostVoteAsync(PostVote vote, CancellationToken ct = default)
    {
        db.PostVotes.Add(vote);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            db.Entry(vote).State = EntityState.Detached;
            return false;
        }
    }

    public async Task RemovePostVoteAsync(PostVote vote, CancellationToken ct = default)
    {
        db.PostVotes.Remove(vote);
        await db.SaveChangesAsync(ct);
    }

    public Task<VoteCounts> CountPostVotesAsync(int postId, CancellationToken ct = default) =>
        AggregateAsync(db.PostVotes.Where(v => v.PostId == postId).Select(v => v.Value), ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);

    // Counts up/down in a single grouped round-trip rather than two COUNT queries.
    private static async Task<VoteCounts> AggregateAsync(IQueryable<short> values, CancellationToken ct)
    {
        var grouped = await values
            .GroupBy(v => v)
            .Select(g => new { Value = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var up = grouped.FirstOrDefault(g => g.Value == 1)?.Count ?? 0;
        var down = grouped.FirstOrDefault(g => g.Value == -1)?.Count ?? 0;
        return new VoteCounts(up, down);
    }

    // SQL Server: 2627 = unique constraint violation, 2601 = duplicate key in a unique index.
    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sql && (sql.Number == 2627 || sql.Number == 2601);
}

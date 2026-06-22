// Forum.Api/Features/Votes/IVoteRepository.cs
namespace Forum.Api.Features.Votes;

/// <summary>Aggregated vote counts for a single target.</summary>
public readonly record struct VoteCounts(int UpVotes, int DownVotes)
{
    public int Score => UpVotes - DownVotes;
}

public interface IVoteRepository
{
    // --- Thread votes ---
    Task<bool> ThreadExistsAsync(int threadId, CancellationToken ct = default);

    /// <summary>Tracked fetch of the caller's existing thread vote (for upsert). Null if none.</summary>
    Task<ThreadVote?> GetThreadVoteAsync(int threadId, int userId, CancellationToken ct = default);

    /// <summary>
    /// Inserts a thread vote. Returns false if a concurrent request already inserted a
    /// vote for the same (thread, user) — i.e. the unique index rejected this insert —
    /// so the caller can fall back to an update. True on a successful insert.
    /// </summary>
    Task<bool> TryAddThreadVoteAsync(ThreadVote vote, CancellationToken ct = default);
    Task RemoveThreadVoteAsync(ThreadVote vote, CancellationToken ct = default);
    Task<VoteCounts> CountThreadVotesAsync(int threadId, CancellationToken ct = default);

    // --- Post votes ---
    Task<bool> PostExistsAsync(int postId, CancellationToken ct = default);

    /// <summary>Tracked fetch of the caller's existing post vote (for upsert). Null if none.</summary>
    Task<PostVote?> GetPostVoteAsync(int postId, int userId, CancellationToken ct = default);

    /// <summary>
    /// Inserts a post vote. Returns false if a concurrent request already inserted a
    /// vote for the same (post, user) — i.e. the unique index rejected this insert —
    /// so the caller can fall back to an update. True on a successful insert.
    /// </summary>
    Task<bool> TryAddPostVoteAsync(PostVote vote, CancellationToken ct = default);
    Task RemovePostVoteAsync(PostVote vote, CancellationToken ct = default);
    Task<VoteCounts> CountPostVotesAsync(int postId, CancellationToken ct = default);

    /// <summary>Persists pending changes to a tracked vote returned by GetThreadVoteAsync/GetPostVoteAsync.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}

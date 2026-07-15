// Forum.Api/Features/Votes/IVoteRepository.cs
namespace Forum.Api.Features.Votes;

/// <summary>Aggregated vote counts for a single target.</summary>
public readonly record struct VoteCounts(int UpVotes, int DownVotes)
{
    public int Score => UpVotes - DownVotes;
}

/// <summary>
/// Who to notify about a vote and where the notification should link: the
/// target's author plus the surrounding thread (a post's target thread is its
/// parent; a thread's is itself).
/// </summary>
public readonly record struct VoteTarget(int AuthorId, int ThreadId, string ThreadTitle);

public interface IVoteRepository
{
    // --- Thread votes ---
    Task<bool> ThreadExistsAsync(int threadId, CancellationToken ct = default);

    /// <summary>Author + thread info for a vote on a thread. Null when the thread is missing.</summary>
    Task<VoteTarget?> GetThreadTargetAsync(int threadId, CancellationToken ct = default);

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

    /// <summary>Author + parent-thread info for a vote on a post. Null when the post is missing.</summary>
    Task<VoteTarget?> GetPostTargetAsync(int postId, CancellationToken ct = default);

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

    /// <summary>
    /// Stops tracking an entity so a subsequent insert of the same (target, user) is not
    /// blocked by leftover tracked state. Used when an update lost a concurrency race and
    /// the service falls back to a fresh insert.
    /// </summary>
    void Detach(object entity);
}

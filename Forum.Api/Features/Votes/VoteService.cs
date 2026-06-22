// Forum.Api/Features/Votes/VoteService.cs
namespace Forum.Api.Features.Votes;

/// <summary>
/// Vote business logic for both threads and posts. A user has at most one vote per
/// target: casting a new value upserts it, casting the same value again toggles it off.
/// There is no ownership restriction — any signed-in user may vote on any target
/// (including their own content), matching typical forum behaviour.
/// </summary>
public sealed class VoteService(IVoteRepository repository) : IVoteService
{
    // ===== Threads =====

    public async Task<VoteResult<VoteTallyResponse>> GetThreadTallyAsync(
        int threadId, int? currentUserId, CancellationToken ct = default)
    {
        if (!await repository.ThreadExistsAsync(threadId, ct))
            return VoteResult<VoteTallyResponse>.Fail(VoteError.NotFound);

        var myVote = currentUserId is int uid
            ? (await repository.GetThreadVoteAsync(threadId, uid, ct))?.Value ?? 0
            : 0;

        return VoteResult<VoteTallyResponse>.Success(
            await BuildTallyAsync(repository.CountThreadVotesAsync(threadId, ct), myVote));
    }

    public async Task<VoteResult<VoteTallyResponse>> CastThreadVoteAsync(
        int threadId, int value, int currentUserId, CancellationToken ct = default)
    {
        if (!await repository.ThreadExistsAsync(threadId, ct))
            return VoteResult<VoteTallyResponse>.Fail(VoteError.NotFound);

        var existing = await repository.GetThreadVoteAsync(threadId, currentUserId, ct);
        var myVote = await ApplyThreadVoteAsync(threadId, currentUserId, (short)value, existing, ct);

        return VoteResult<VoteTallyResponse>.Success(
            await BuildTallyAsync(repository.CountThreadVotesAsync(threadId, ct), myVote));
    }

    public async Task<VoteResult<VoteTallyResponse>> ClearThreadVoteAsync(
        int threadId, int currentUserId, CancellationToken ct = default)
    {
        if (!await repository.ThreadExistsAsync(threadId, ct))
            return VoteResult<VoteTallyResponse>.Fail(VoteError.NotFound);

        var existing = await repository.GetThreadVoteAsync(threadId, currentUserId, ct);
        if (existing is not null)
            await repository.RemoveThreadVoteAsync(existing, ct);

        return VoteResult<VoteTallyResponse>.Success(
            await BuildTallyAsync(repository.CountThreadVotesAsync(threadId, ct), 0));
    }

    private async Task<int> ApplyThreadVoteAsync(
        int threadId, int userId, short value, ThreadVote? existing, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        if (existing is null)
        {
            var inserted = await repository.TryAddThreadVoteAsync(new ThreadVote
            {
                ThreadId = threadId,
                UserId = userId,
                Value = value,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }, ct);

            if (inserted)
                return value;

            // A concurrent request inserted first; re-read and fall through to update/toggle.
            existing = await repository.GetThreadVoteAsync(threadId, userId, ct);
            if (existing is null)
                return value; // gone again (deleted concurrently) — nothing more we can do safely
        }

        if (existing.Value == value)
        {
            // Same value again → toggle the vote off.
            await repository.RemoveThreadVoteAsync(existing, ct);
            return 0;
        }

        existing.Value = value;
        existing.UpdatedAtUtc = now;
        await repository.SaveChangesAsync(ct);
        return value;
    }

    // ===== Posts =====

    public async Task<VoteResult<VoteTallyResponse>> GetPostTallyAsync(
        int postId, int? currentUserId, CancellationToken ct = default)
    {
        if (!await repository.PostExistsAsync(postId, ct))
            return VoteResult<VoteTallyResponse>.Fail(VoteError.NotFound);

        var myVote = currentUserId is int uid
            ? (await repository.GetPostVoteAsync(postId, uid, ct))?.Value ?? 0
            : 0;

        return VoteResult<VoteTallyResponse>.Success(
            await BuildTallyAsync(repository.CountPostVotesAsync(postId, ct), myVote));
    }

    public async Task<VoteResult<VoteTallyResponse>> CastPostVoteAsync(
        int postId, int value, int currentUserId, CancellationToken ct = default)
    {
        if (!await repository.PostExistsAsync(postId, ct))
            return VoteResult<VoteTallyResponse>.Fail(VoteError.NotFound);

        var existing = await repository.GetPostVoteAsync(postId, currentUserId, ct);
        var myVote = await ApplyPostVoteAsync(postId, currentUserId, (short)value, existing, ct);

        return VoteResult<VoteTallyResponse>.Success(
            await BuildTallyAsync(repository.CountPostVotesAsync(postId, ct), myVote));
    }

    public async Task<VoteResult<VoteTallyResponse>> ClearPostVoteAsync(
        int postId, int currentUserId, CancellationToken ct = default)
    {
        if (!await repository.PostExistsAsync(postId, ct))
            return VoteResult<VoteTallyResponse>.Fail(VoteError.NotFound);

        var existing = await repository.GetPostVoteAsync(postId, currentUserId, ct);
        if (existing is not null)
            await repository.RemovePostVoteAsync(existing, ct);

        return VoteResult<VoteTallyResponse>.Success(
            await BuildTallyAsync(repository.CountPostVotesAsync(postId, ct), 0));
    }

    private async Task<int> ApplyPostVoteAsync(
        int postId, int userId, short value, PostVote? existing, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        if (existing is null)
        {
            var inserted = await repository.TryAddPostVoteAsync(new PostVote
            {
                PostId = postId,
                UserId = userId,
                Value = value,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }, ct);

            if (inserted)
                return value;

            // A concurrent request inserted first; re-read and fall through to update/toggle.
            existing = await repository.GetPostVoteAsync(postId, userId, ct);
            if (existing is null)
                return value; // gone again (deleted concurrently) — nothing more we can do safely
        }

        if (existing.Value == value)
        {
            await repository.RemovePostVoteAsync(existing, ct);
            return 0;
        }

        existing.Value = value;
        existing.UpdatedAtUtc = now;
        await repository.SaveChangesAsync(ct);
        return value;
    }

    // ===== Shared =====

    private static async Task<VoteTallyResponse> BuildTallyAsync(Task<VoteCounts> countsTask, int myVote)
    {
        var counts = await countsTask;
        return new VoteTallyResponse(counts.Score, counts.UpVotes, counts.DownVotes, myVote);
    }
}

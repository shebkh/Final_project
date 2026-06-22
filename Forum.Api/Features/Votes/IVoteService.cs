// Forum.Api/Features/Votes/IVoteService.cs
namespace Forum.Api.Features.Votes;

/// <summary>
/// Outcome classification for vote operations. Keeps HTTP-status decisions
/// in the controller while the service expresses domain results.
/// </summary>
public enum VoteError
{
    None = 0,
    NotFound   // the target thread/post does not exist
}

public readonly record struct VoteResult<T>(T? Value, VoteError Error) where T : class
{
    public bool Succeeded => Error == VoteError.None;

    public static VoteResult<T> Success(T value) => new(value, VoteError.None);
    public static VoteResult<T> Fail(VoteError error) => new(null, error);
}

public interface IVoteService
{
    // --- Threads ---
    Task<VoteResult<VoteTallyResponse>> GetThreadTallyAsync(int threadId, int? currentUserId, CancellationToken ct = default);
    Task<VoteResult<VoteTallyResponse>> CastThreadVoteAsync(int threadId, int value, int currentUserId, CancellationToken ct = default);
    Task<VoteResult<VoteTallyResponse>> ClearThreadVoteAsync(int threadId, int currentUserId, CancellationToken ct = default);

    // --- Posts ---
    Task<VoteResult<VoteTallyResponse>> GetPostTallyAsync(int postId, int? currentUserId, CancellationToken ct = default);
    Task<VoteResult<VoteTallyResponse>> CastPostVoteAsync(int postId, int value, int currentUserId, CancellationToken ct = default);
    Task<VoteResult<VoteTallyResponse>> ClearPostVoteAsync(int postId, int currentUserId, CancellationToken ct = default);
}

// Forum.Api/Features/Votes/VoteDtos.cs
namespace Forum.Api.Features.Votes;

/// <summary>Request body for PUT /api/threads/{id}/vote and /api/posts/{id}/vote.</summary>
public record CastVoteRequest(int Value);

/// <summary>
/// The vote tally for a target plus the current caller's own vote.
/// MyVote is 0 for an anonymous caller or a caller who hasn't voted.
/// </summary>
public record VoteTallyResponse(
    int Score,      // upVotes - downVotes
    int UpVotes,
    int DownVotes,
    int MyVote);    // -1, 0, or +1

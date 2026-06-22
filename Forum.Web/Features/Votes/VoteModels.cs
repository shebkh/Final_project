// Forum.Web/Features/Votes/VoteModels.cs
namespace Forum.Web.Features.Votes;

/// <summary>Identifies which kind of target a VoteBox votes on.</summary>
public enum VoteTargetKind
{
    Thread,
    Post
}

// --- Wire DTOs matching the API's request/response shapes ---

public record CastVoteRequest(int Value);

public record VoteTallyResponse(int Score, int UpVotes, int DownVotes, int MyVote);

/// <summary>Result wrapper so components handle failures without exceptions.</summary>
public record VoteOutcome(bool Succeeded, VoteTallyResponse? Data, string? Error)
{
    public static VoteOutcome Ok(VoteTallyResponse data) => new(true, data, null);
    public static VoteOutcome Failed(string error) => new(false, null, error);
}

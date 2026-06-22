// Forum.Api/Features/Votes/PostVote.cs
using Forum.Api.Features.Auth;
using Forum.Api.Features.Posts;

namespace Forum.Api.Features.Votes;

/// <summary>
/// A single user's vote on a post (reply). One row per (PostId, UserId) — enforced by a
/// unique index. Value is +1 (up) or -1 (down). Never exposed directly over the API.
/// </summary>
public class PostVote
{
    public int Id { get; set; }

    /// <summary>FK to the voted-on post.</summary>
    public int PostId { get; set; }
    public Post? Post { get; set; }

    /// <summary>FK to the voting user.</summary>
    public int UserId { get; set; }
    public User? User { get; set; }

    /// <summary>+1 for an up-vote, -1 for a down-vote.</summary>
    public short Value { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Set whenever the vote value changes; equals CreatedAtUtc on creation.</summary>
    public DateTime UpdatedAtUtc { get; set; }
}

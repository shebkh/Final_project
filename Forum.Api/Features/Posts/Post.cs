// Forum.Api/Features/Posts/Post.cs
using Forum.Api.Features.Auth;
using Forum.Api.Features.Threads;

namespace Forum.Api.Features.Posts;

/// <summary>
/// EF Core entity for a reply (post) on a discussion thread.
/// Never exposed directly over the API — always projected to a DTO.
/// </summary>
public class Post
{
    public int Id { get; set; }

    public required string Body { get; set; }

    /// <summary>FK to the parent thread this reply belongs to.</summary>
    public int ThreadId { get; set; }

    /// <summary>Navigation to the parent thread. Loaded explicitly when needed.</summary>
    public ForumThread? Thread { get; set; }

    /// <summary>FK to the User who wrote the reply.</summary>
    public int AuthorId { get; set; }

    /// <summary>Navigation to the author. Loaded explicitly when needed.</summary>
    public User? Author { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Set whenever the reply is edited; equals CreatedAtUtc on creation.</summary>
    public DateTime UpdatedAtUtc { get; set; }
}

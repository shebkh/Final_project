// Forum.Api/Features/Threads/ForumThread.cs
using Forum.Api.Features.Auth;

namespace Forum.Api.Features.Threads;

/// <summary>
/// EF Core entity for a discussion thread. Named ForumThread to avoid colliding
/// with System.Threading.Thread; the database table is "Threads".
/// Never exposed directly over the API — always projected to a DTO.
/// </summary>
public class ForumThread
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public required string Body { get; set; }

    /// <summary>FK to the User who created the thread.</summary>
    public int AuthorId { get; set; }

    /// <summary>Navigation to the author. Loaded explicitly when needed.</summary>
    public User? Author { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Set whenever the thread's content is edited; equals CreatedAtUtc on creation.</summary>
    public DateTime UpdatedAtUtc { get; set; }
}

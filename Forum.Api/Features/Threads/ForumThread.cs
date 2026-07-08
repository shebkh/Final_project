// Forum.Api/Features/Threads/ForumThread.cs
using Forum.Api.Features.Auth;
using Forum.Api.Features.Categories;

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

    /// <summary>
    /// Whether a moderator has pinned this thread. Pinned threads sort ahead of the rest
    /// in the listing. Set only via the Moderation slice. Defaults to false.
    /// </summary>
    public bool IsPinned { get; set; }

    /// <summary>
    /// Whether a moderator has locked this thread. Locked threads reject new replies and
    /// reply edits. Set only via the Moderation slice. Defaults to false.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// FK to the category this thread is filed under; null = uncategorized.
    /// Set at creation/edit by the author, or via the Moderation slice's move action.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>Navigation to the category. Loaded explicitly when needed.</summary>
    public Category? Category { get; set; }

    /// <summary>FK to the User who created the thread.</summary>
    public int AuthorId { get; set; }

    /// <summary>Navigation to the author. Loaded explicitly when needed.</summary>
    public User? Author { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Set whenever the thread's content is edited; equals CreatedAtUtc on creation.</summary>
    public DateTime UpdatedAtUtc { get; set; }
}

// Forum.Api/Features/Threads/ThreadTag.cs
namespace Forum.Api.Features.Threads;

/// <summary>
/// Join entity linking a thread to a tag (composite key ThreadId + TagId).
/// Rows are replaced wholesale when a thread's tags change; both FKs cascade
/// so join rows die with either side.
/// </summary>
public class ThreadTag
{
    public int ThreadId { get; set; }
    public ForumThread? Thread { get; set; }

    public int TagId { get; set; }
    public Tag? Tag { get; set; }
}

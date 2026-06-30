// Forum.Api/Features/Moderation/IModerationRepository.cs
using Forum.Api.Features.Threads;

namespace Forum.Api.Features.Moderation;

public interface IModerationRepository
{
    /// <summary>
    /// Tracked fetch of a thread WITHOUT navigations, for toggling moderation flags.
    /// Mutating + SaveChangesAsync emits a minimal UPDATE confined to the Threads row.
    /// Null if not found.
    /// </summary>
    Task<ForumThread?> GetThreadForUpdateAsync(int threadId, CancellationToken ct = default);

    /// <summary>Persists pending changes to a tracked thread returned by GetThreadForUpdateAsync.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}

// Forum.Api/Features/Moderation/ModerationRepository.cs
using Forum.Api.Data;
using Forum.Api.Features.Threads;
using Microsoft.EntityFrameworkCore;

namespace Forum.Api.Features.Moderation;

public sealed class ModerationRepository(AppDbContext db) : IModerationRepository
{
    // Tracked, no Include — so SaveChanges writes only the Threads row when a flag changes.
    public Task<ForumThread?> GetThreadForUpdateAsync(int threadId, CancellationToken ct = default) =>
        db.Threads.FirstOrDefaultAsync(t => t.Id == threadId, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}

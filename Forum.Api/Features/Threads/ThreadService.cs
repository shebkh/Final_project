// Forum.Api/Features/Threads/ThreadService.cs
namespace Forum.Api.Features.Threads;

public sealed class ThreadService(IThreadRepository repository) : IThreadService
{
    private const int MaxPageSize = 50;
    private const int ExcerptLength = 200;

    public async Task<IReadOnlyList<ThreadSummaryResponse>> ListAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var (skip, take) = Normalize(page, pageSize);
        var threads = await repository.ListAsync(skip, take, ct);
        return threads.Select(ToSummary).ToList();
    }

    public Task<int> CountAsync(CancellationToken ct = default) =>
        repository.CountAsync(ct);

    public async Task<ThreadResult<ThreadDetailResponse>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var thread = await repository.GetByIdAsync(id, ct);
        return thread is null
            ? ThreadResult<ThreadDetailResponse>.Fail(ThreadError.NotFound)
            : ThreadResult<ThreadDetailResponse>.Success(ToDetail(thread));
    }

    public async Task<ThreadDetailResponse> CreateAsync(
        CreateThreadRequest request, int authorId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var thread = new ForumThread
        {
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            AuthorId = authorId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await repository.AddAsync(thread, ct);

        // Re-read so the Author navigation is populated for the response.
        var created = await repository.GetByIdAsync(thread.Id, ct);
        return ToDetail(created!);
    }

    public async Task<ThreadResult<ThreadDetailResponse>> UpdateAsync(
        int id, UpdateThreadRequest request, int currentUserId, CancellationToken ct = default)
    {
        // Tracked, no-Include fetch: mutating + SaveChanges writes only the Threads row.
        var thread = await repository.GetForUpdateAsync(id, ct);
        if (thread is null)
            return ThreadResult<ThreadDetailResponse>.Fail(ThreadError.NotFound);

        if (thread.AuthorId != currentUserId)
            return ThreadResult<ThreadDetailResponse>.Fail(ThreadError.Forbidden);

        thread.Title = request.Title.Trim();
        thread.Body = request.Body.Trim();
        thread.UpdatedAtUtc = DateTime.UtcNow;

        await repository.SaveChangesAsync(ct);

        // Re-read with the Author navigation for the response projection.
        var updated = await repository.GetByIdAsync(id, ct);
        return ThreadResult<ThreadDetailResponse>.Success(ToDetail(updated!));
    }

    public async Task<ThreadResult<ThreadDetailResponse>> DeleteAsync(
        int id, int currentUserId, bool isModerator, CancellationToken ct = default)
    {
        // Read-path fetch first so the snapshot includes the Author for the response.
        var snapshotSource = await repository.GetByIdAsync(id, ct);
        if (snapshotSource is null)
            return ThreadResult<ThreadDetailResponse>.Fail(ThreadError.NotFound);

        // Owner or moderator may delete the thread.
        if (snapshotSource.AuthorId != currentUserId && !isModerator)
            return ThreadResult<ThreadDetailResponse>.Fail(ThreadError.Forbidden);

        var snapshot = ToDetail(snapshotSource);

        // Re-fetch tracked (no Include) so Remove deletes only the Threads row.
        var tracked = await repository.GetForUpdateAsync(id, ct);
        if (tracked is null)
            return ThreadResult<ThreadDetailResponse>.Fail(ThreadError.NotFound);

        await repository.DeleteAsync(tracked, ct);
        return ThreadResult<ThreadDetailResponse>.Success(snapshot);
    }

    private static (int skip, int take) Normalize(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;
        return ((page - 1) * pageSize, pageSize);
    }

    private static string BuildExcerpt(string body) =>
        body.Length <= ExcerptLength ? body : body[..ExcerptLength].TrimEnd() + "…";

    private static ThreadSummaryResponse ToSummary(ForumThread t) => new(
        t.Id,
        t.Title,
        BuildExcerpt(t.Body),
        t.AuthorId,
        t.Author?.UserName ?? "(unknown)",
        t.CreatedAtUtc,
        t.UpdatedAtUtc,
        t.IsPinned,
        t.IsLocked);

    private static ThreadDetailResponse ToDetail(ForumThread t) => new(
        t.Id,
        t.Title,
        t.Body,
        t.AuthorId,
        t.Author?.UserName ?? "(unknown)",
        t.CreatedAtUtc,
        t.UpdatedAtUtc,
        t.IsPinned,
        t.IsLocked);
}

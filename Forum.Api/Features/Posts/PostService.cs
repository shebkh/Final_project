// Forum.Api/Features/Posts/PostService.cs
using Forum.Api.Features.Notifications;

namespace Forum.Api.Features.Posts;

public sealed class PostService(
    IPostRepository repository,
    INotificationService notifications) : IPostService
{
    private const int MaxPageSize = 50;

    public async Task<PostResult<PagedPosts>> ListByThreadAsync(
        int threadId, int page, int pageSize, CancellationToken ct = default)
    {
        if (!await repository.ThreadExistsAsync(threadId, ct))
            return PostResult<PagedPosts>.Fail(PostError.ThreadNotFound);

        var (skip, take) = Normalize(page, pageSize);
        var posts = await repository.ListByThreadAsync(threadId, skip, take, ct);
        var total = await repository.CountByThreadAsync(threadId, ct);
        IReadOnlyList<PostResponse> items = posts.Select(ToResponse).ToList();
        return PostResult<PagedPosts>.Success(new PagedPosts(items, total));
    }

    public async Task<PostResult<PostResponse>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var post = await repository.GetByIdAsync(id, ct);
        return post is null
            ? PostResult<PostResponse>.Fail(PostError.NotFound)
            : PostResult<PostResponse>.Success(ToResponse(post));
    }

    public async Task<PostResult<PostResponse>> CreateAsync(
        int threadId, CreatePostRequest request, int authorId, CancellationToken ct = default)
    {
        var thread = await repository.GetThreadInfoAsync(threadId, ct);
        if (thread is not ThreadInfo info)
            return PostResult<PostResponse>.Fail(PostError.ThreadNotFound);
        if (info.IsLocked)
            return PostResult<PostResponse>.Fail(PostError.ThreadLocked);

        var now = DateTime.UtcNow;
        var post = new Post
        {
            Body = request.Body.Trim(),
            ThreadId = threadId,
            AuthorId = authorId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await repository.AddAsync(post, ct);

        // Re-read so the Author navigation is populated for the response.
        var created = await repository.GetByIdAsync(post.Id, ct);

        // Real-time nudge for the thread author — skipped when replying to yourself.
        if (info.AuthorId != authorId)
            await notifications.NotifyThreadRepliedAsync(
                info.AuthorId, created!.Author?.UserName ?? "Someone", threadId, info.Title, ct);

        return PostResult<PostResponse>.Success(ToResponse(created!));
    }

    public async Task<PostResult<PostResponse>> UpdateAsync(
        int id, UpdatePostRequest request, int currentUserId, bool isModerator, CancellationToken ct = default)
    {
        // Tracked, no-Include fetch: mutating + SaveChanges writes only the Posts row.
        var post = await repository.GetForUpdateAsync(id, ct);
        if (post is null)
            return PostResult<PostResponse>.Fail(PostError.NotFound);

        // Owner or moderator may edit. (Moderators editing others' replies is unusual but
        // consistent with delete-any; the owner check is the primary gate.)
        if (post.AuthorId != currentUserId && !isModerator)
            return PostResult<PostResponse>.Fail(PostError.Forbidden);

        // A locked thread freezes its replies — no further edits until unlocked.
        var (_, isLocked) = await repository.GetThreadLockStateAsync(post.ThreadId, ct);
        if (isLocked)
            return PostResult<PostResponse>.Fail(PostError.ThreadLocked);

        post.Body = request.Body.Trim();
        post.UpdatedAtUtc = DateTime.UtcNow;

        await repository.SaveChangesAsync(ct);

        var updated = await repository.GetByIdAsync(id, ct);
        return PostResult<PostResponse>.Success(ToResponse(updated!));
    }

    public async Task<PostResult<PostResponse>> DeleteAsync(
        int id, int currentUserId, bool isModerator, CancellationToken ct = default)
    {
        // Read-path fetch first so the snapshot includes the Author for the response.
        var snapshotSource = await repository.GetByIdAsync(id, ct);
        if (snapshotSource is null)
            return PostResult<PostResponse>.Fail(PostError.NotFound);

        // Owner or moderator may delete. Moderators may delete on locked threads too —
        // removing spam/abuse from a locked thread is a core moderation use case.
        if (snapshotSource.AuthorId != currentUserId && !isModerator)
            return PostResult<PostResponse>.Fail(PostError.Forbidden);

        var snapshot = ToResponse(snapshotSource);

        // Re-fetch tracked (no Include) so Remove deletes only the Posts row.
        var tracked = await repository.GetForUpdateAsync(id, ct);
        if (tracked is null)
            return PostResult<PostResponse>.Fail(PostError.NotFound);

        await repository.DeleteAsync(tracked, ct);
        return PostResult<PostResponse>.Success(snapshot);
    }

    private static (int skip, int take) Normalize(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;
        return ((page - 1) * pageSize, pageSize);
    }

    private static PostResponse ToResponse(Post p) => new(
        p.Id,
        p.ThreadId,
        p.Body,
        p.AuthorId,
        p.Author?.UserName ?? "(unknown)",
        p.CreatedAtUtc,
        p.UpdatedAtUtc);
}

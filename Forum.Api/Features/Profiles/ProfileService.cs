// Forum.Api/Features/Profiles/ProfileService.cs
using Forum.Api.Features.Posts;
using Forum.Api.Features.Threads;

namespace Forum.Api.Features.Profiles;

public sealed class ProfileService(IProfileRepository repository) : IProfileService
{
    private const int MaxPageSize = 50;
    private const int ExcerptLength = 200;

    public async Task<ProfileResult<UserProfileResponse>> GetProfileAsync(
        int userId, CancellationToken ct = default)
    {
        var user = await repository.GetUserAsync(userId, ct);
        if (user is null)
            return ProfileResult<UserProfileResponse>.Fail(ProfileError.NotFound);

        var threadCount = await repository.CountThreadsByAuthorAsync(userId, ct);
        var postCount = await repository.CountPostsByAuthorAsync(userId, ct);
        var reputation = await repository.GetReputationAsync(userId, ct);

        var profile = new UserProfileResponse(
            user.Id,
            user.UserName,
            user.CreatedAtUtc,
            threadCount,
            postCount,
            reputation);

        return ProfileResult<UserProfileResponse>.Success(profile);
    }

    public async Task<ProfileResult<PagedProfileThreads>> ListThreadsAsync(
        int userId, int page, int pageSize, CancellationToken ct = default)
    {
        if (!await repository.UserExistsAsync(userId, ct))
            return ProfileResult<PagedProfileThreads>.Fail(ProfileError.NotFound);

        var (skip, take) = Normalize(page, pageSize);
        var threads = await repository.ListThreadsByAuthorAsync(userId, skip, take, ct);
        var total = await repository.CountThreadsByAuthorAsync(userId, ct);

        var items = threads.Select(ToThreadSummary).ToList();
        return ProfileResult<PagedProfileThreads>.Success(new PagedProfileThreads(items, total));
    }

    public async Task<ProfileResult<PagedProfilePosts>> ListPostsAsync(
        int userId, int page, int pageSize, CancellationToken ct = default)
    {
        if (!await repository.UserExistsAsync(userId, ct))
            return ProfileResult<PagedProfilePosts>.Fail(ProfileError.NotFound);

        var (skip, take) = Normalize(page, pageSize);
        var posts = await repository.ListPostsByAuthorAsync(userId, skip, take, ct);
        var total = await repository.CountPostsByAuthorAsync(userId, ct);

        var items = posts.Select(ToPostSummary).ToList();
        return ProfileResult<PagedProfilePosts>.Success(new PagedProfilePosts(items, total));
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

    private static ProfileThreadResponse ToThreadSummary(ForumThread t) => new(
        t.Id,
        t.Title,
        BuildExcerpt(t.Body),
        t.CreatedAtUtc,
        t.UpdatedAtUtc);

    private static ProfilePostResponse ToPostSummary(Post p) => new(
        p.Id,
        p.ThreadId,
        p.Thread?.Title ?? "(deleted thread)",
        BuildExcerpt(p.Body),
        p.CreatedAtUtc,
        p.UpdatedAtUtc);
}

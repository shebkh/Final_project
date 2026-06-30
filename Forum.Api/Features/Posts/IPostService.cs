// Forum.Api/Features/Posts/IPostService.cs
namespace Forum.Api.Features.Posts;

/// <summary>
/// Outcome classification for post operations. Keeps HTTP-status decisions
/// in the controller while the service expresses domain results.
/// </summary>
public enum PostError
{
    None = 0,
    ThreadNotFound,
    NotFound,
    Forbidden,
    ThreadLocked
}

/// <summary>Generic result carrying an optional payload plus an error classification.</summary>
public readonly record struct PostResult<T>(T? Value, PostError Error) where T : class
{
    public bool Succeeded => Error == PostError.None;

    public static PostResult<T> Success(T value) => new(value, PostError.None);
    public static PostResult<T> Fail(PostError error) => new(null, error);
}

public interface IPostService
{
    Task<PostResult<PagedPosts>> ListByThreadAsync(
        int threadId, int page, int pageSize, CancellationToken ct = default);

    Task<PostResult<PostResponse>> GetByIdAsync(int id, CancellationToken ct = default);

    Task<PostResult<PostResponse>> CreateAsync(
        int threadId, CreatePostRequest request, int authorId, CancellationToken ct = default);

    Task<PostResult<PostResponse>> UpdateAsync(
        int id, UpdatePostRequest request, int currentUserId, bool isModerator, CancellationToken ct = default);

    Task<PostResult<PostResponse>> DeleteAsync(
        int id, int currentUserId, bool isModerator, CancellationToken ct = default);
}

// Forum.Api/Features/Posts/PostDtos.cs
namespace Forum.Api.Features.Posts;

/// <summary>Request body for POST /api/threads/{threadId}/posts.</summary>
public record CreatePostRequest(string Body);

/// <summary>Request body for PUT /api/posts/{id}.</summary>
public record UpdatePostRequest(string Body);

/// <summary>Projection for a reply returned to clients.</summary>
public record PostResponse(
    int Id,
    int ThreadId,
    string Body,
    int AuthorId,
    string AuthorUserName,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

/// <summary>A page of replies plus the total count, returned together for consistency.</summary>
public record PagedPosts(IReadOnlyList<PostResponse> Items, int Total);

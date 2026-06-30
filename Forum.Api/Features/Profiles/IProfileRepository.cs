// Forum.Api/Features/Profiles/IProfileRepository.cs
using Forum.Api.Features.Auth;
using Forum.Api.Features.Posts;
using Forum.Api.Features.Threads;

namespace Forum.Api.Features.Profiles;

public interface IProfileRepository
{
    /// <summary>True if a user with this id exists.</summary>
    Task<bool> UserExistsAsync(int userId, CancellationToken ct = default);

    /// <summary>The user record, read-only. Null if not found.</summary>
    Task<User?> GetUserAsync(int userId, CancellationToken ct = default);

    /// <summary>Count of threads authored by the user.</summary>
    Task<int> CountThreadsByAuthorAsync(int userId, CancellationToken ct = default);

    /// <summary>Count of posts authored by the user.</summary>
    Task<int> CountPostsByAuthorAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// The user's reputation: the summed vote value (+1 up / -1 down) across every vote cast
    /// on threads AND posts the user authored. Zero when they have no voted-on content.
    /// </summary>
    Task<int> GetReputationAsync(int userId, CancellationToken ct = default);

    /// <summary>Newest-first page of the user's authored threads, read-only.</summary>
    Task<IReadOnlyList<ForumThread>> ListThreadsByAuthorAsync(
        int userId, int skip, int take, CancellationToken ct = default);

    /// <summary>Newest-first page of the user's authored posts with the parent Thread loaded, read-only.</summary>
    Task<IReadOnlyList<Post>> ListPostsByAuthorAsync(
        int userId, int skip, int take, CancellationToken ct = default);
}

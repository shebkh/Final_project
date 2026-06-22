// Forum.Api/Features/Profiles/IProfileService.cs
namespace Forum.Api.Features.Profiles;

/// <summary>
/// Outcome classification for profile reads. Profiles are anonymous reads, so the
/// only domain failure is a missing user; the controller maps it to 404.
/// </summary>
public enum ProfileError
{
    None = 0,
    NotFound
}

/// <summary>Generic result carrying an optional payload plus an error classification.</summary>
public readonly record struct ProfileResult<T>(T? Value, ProfileError Error) where T : class
{
    public bool Succeeded => Error == ProfileError.None;

    public static ProfileResult<T> Success(T value) => new(value, ProfileError.None);
    public static ProfileResult<T> Fail(ProfileError error) => new(null, error);
}

public interface IProfileService
{
    Task<ProfileResult<UserProfileResponse>> GetProfileAsync(int userId, CancellationToken ct = default);

    Task<ProfileResult<PagedProfileThreads>> ListThreadsAsync(
        int userId, int page, int pageSize, CancellationToken ct = default);

    Task<ProfileResult<PagedProfilePosts>> ListPostsAsync(
        int userId, int page, int pageSize, CancellationToken ct = default);
}

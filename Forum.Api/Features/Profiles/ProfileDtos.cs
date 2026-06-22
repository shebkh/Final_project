// Forum.Api/Features/Profiles/ProfileDtos.cs
namespace Forum.Api.Features.Profiles;

/// <summary>Public profile projection for a user. No email or hash — public read.</summary>
public record UserProfileResponse(
    int Id,
    string UserName,
    DateTime JoinedAtUtc,
    int ThreadCount,
    int PostCount);

/// <summary>One thread in a user's authored-thread history.</summary>
public record ProfileThreadResponse(
    int Id,
    string Title,
    string Excerpt,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

/// <summary>One reply in a user's authored-post history, with its parent thread for context.</summary>
public record ProfilePostResponse(
    int Id,
    int ThreadId,
    string ThreadTitle,
    string Excerpt,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

/// <summary>A page of items plus the total count, returned together for consistency.</summary>
public record PagedProfileThreads(IReadOnlyList<ProfileThreadResponse> Items, int Total);

/// <summary>A page of items plus the total count, returned together for consistency.</summary>
public record PagedProfilePosts(IReadOnlyList<ProfilePostResponse> Items, int Total);

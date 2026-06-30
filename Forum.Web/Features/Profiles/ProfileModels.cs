// Forum.Web/Features/Profiles/ProfileModels.cs
namespace Forum.Web.Features.Profiles;

// --- Wire DTOs matching the API's response shapes ---

public record UserProfileResponse(
    int Id,
    string UserName,
    DateTime JoinedAtUtc,
    int ThreadCount,
    int PostCount,
    int Reputation);

public record ProfileThreadResponse(
    int Id,
    string Title,
    string Excerpt,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record ProfilePostResponse(
    int Id,
    int ThreadId,
    string ThreadTitle,
    string Excerpt,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

/// <summary>Result wrapper so components handle failures without exceptions.</summary>
public record ProfileOutcome<T>(bool Succeeded, T? Data, string? Error) where T : class
{
    public static ProfileOutcome<T> Ok(T data) => new(true, data, null);
    public static ProfileOutcome<T> Failed(string error) => new(false, null, error);
}

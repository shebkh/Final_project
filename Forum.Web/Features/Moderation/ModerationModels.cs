// Forum.Web/Features/Moderation/ModerationModels.cs
namespace Forum.Web.Features.Moderation;

// --- Wire DTOs matching the API's request/response shapes ---

public record SetPinRequest(bool Pinned);
public record SetLockRequest(bool Locked);

public record ThreadModerationResponse(int ThreadId, bool IsPinned, bool IsLocked);

/// <summary>Result wrapper so components handle failures without exceptions.</summary>
public record ModerationOutcome(bool Succeeded, ThreadModerationResponse? Data, string? Error)
{
    public static ModerationOutcome Ok(ThreadModerationResponse data) => new(true, data, null);
    public static ModerationOutcome Failed(string error) => new(false, null, error);
}

// Forum.Api/Features/Moderation/ModerationDtos.cs
namespace Forum.Api.Features.Moderation;

/// <summary>Request body for the pin toggle: PUT /api/moderation/threads/{id}/pin.</summary>
public record SetPinRequest(bool Pinned);

/// <summary>Request body for the lock toggle: PUT /api/moderation/threads/{id}/lock.</summary>
public record SetLockRequest(bool Locked);

/// <summary>The moderation state of a thread after a pin/lock change.</summary>
public record ThreadModerationResponse(int ThreadId, bool IsPinned, bool IsLocked);

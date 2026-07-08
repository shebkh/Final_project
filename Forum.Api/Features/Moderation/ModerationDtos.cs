// Forum.Api/Features/Moderation/ModerationDtos.cs
namespace Forum.Api.Features.Moderation;

/// <summary>Request body for the pin toggle: PUT /api/moderation/threads/{id}/pin.</summary>
public record SetPinRequest(bool Pinned);

/// <summary>Request body for the lock toggle: PUT /api/moderation/threads/{id}/lock.</summary>
public record SetLockRequest(bool Locked);

/// <summary>Request body for moving a thread: PUT /api/moderation/threads/{id}/move. Null = uncategorize.</summary>
public record MoveThreadRequest(int? CategoryId);

/// <summary>The moderation state of a thread after a pin/lock/move change.</summary>
public record ThreadModerationResponse(int ThreadId, bool IsPinned, bool IsLocked, int? CategoryId);

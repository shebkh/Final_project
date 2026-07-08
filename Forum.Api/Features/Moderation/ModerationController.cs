// Forum.Api/Features/Moderation/ModerationController.cs
using Forum.Api.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forum.Api.Features.Moderation;

[ApiController]
[Route("api/moderation")]
[Authorize(Roles = ModeratorRole.Name)]
public sealed class ModerationController(IModerationService moderationService) : ControllerBase
{
    [HttpPut("threads/{threadId:int}/pin")]
    [ProducesResponseType(typeof(ThreadModerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPin(int threadId, SetPinRequest request, CancellationToken ct)
    {
        var result = await moderationService.SetPinnedAsync(threadId, request.Pinned, ct);
        return MapResult(result);
    }

    [HttpPut("threads/{threadId:int}/lock")]
    [ProducesResponseType(typeof(ThreadModerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetLock(int threadId, SetLockRequest request, CancellationToken ct)
    {
        var result = await moderationService.SetLockedAsync(threadId, request.Locked, ct);
        return MapResult(result);
    }

    [HttpPut("threads/{threadId:int}/move")]
    [ProducesResponseType(typeof(ThreadModerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Move(int threadId, MoveThreadRequest request, CancellationToken ct)
    {
        var result = await moderationService.MoveAsync(threadId, request.CategoryId, ct);
        return MapResult(result);
    }

    private IActionResult MapResult(ModerationResult<ThreadModerationResponse> result) => result.Error switch
    {
        ModerationError.None when result.Value is not null => Ok(result.Value),
        ModerationError.ThreadNotFound => NotFound(new { error = "Thread not found." }),
        ModerationError.CategoryNotFound => BadRequest(new { error = "Category not found." }),
        _ => Problem("An unexpected error occurred.")
    };
}

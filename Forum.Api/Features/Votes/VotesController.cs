// Forum.Api/Features/Votes/VotesController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forum.Api.Features.Votes;

[ApiController]
public sealed class VotesController(IVoteService voteService) : ControllerBase
{
    // ===== Thread votes =====

    [HttpGet("api/threads/{threadId:int}/vote")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(VoteTallyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetThreadTally(int threadId, CancellationToken ct)
    {
        var result = await voteService.GetThreadTallyAsync(threadId, TryGetUserId(out var uid) ? uid : null, ct);
        return MapResult(result, "Thread not found.");
    }

    [HttpPut("api/threads/{threadId:int}/vote")]
    [Authorize]
    [ProducesResponseType(typeof(VoteTallyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CastThreadVote(int threadId, CastVoteRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await voteService.CastThreadVoteAsync(threadId, request.Value, userId, ct);
        return MapResult(result, "Thread not found.");
    }

    [HttpDelete("api/threads/{threadId:int}/vote")]
    [Authorize]
    [ProducesResponseType(typeof(VoteTallyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearThreadVote(int threadId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await voteService.ClearThreadVoteAsync(threadId, userId, ct);
        return MapResult(result, "Thread not found.");
    }

    // ===== Post votes =====

    [HttpGet("api/posts/{postId:int}/vote")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(VoteTallyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPostTally(int postId, CancellationToken ct)
    {
        var result = await voteService.GetPostTallyAsync(postId, TryGetUserId(out var uid) ? uid : null, ct);
        return MapResult(result, "Reply not found.");
    }

    [HttpPut("api/posts/{postId:int}/vote")]
    [Authorize]
    [ProducesResponseType(typeof(VoteTallyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CastPostVote(int postId, CastVoteRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await voteService.CastPostVoteAsync(postId, request.Value, userId, ct);
        return MapResult(result, "Reply not found.");
    }

    [HttpDelete("api/posts/{postId:int}/vote")]
    [Authorize]
    [ProducesResponseType(typeof(VoteTallyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearPostVote(int postId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await voteService.ClearPostVoteAsync(postId, userId, ct);
        return MapResult(result, "Reply not found.");
    }

    // ===== Helpers =====

    private IActionResult MapResult(VoteResult<VoteTallyResponse> result, string notFoundMessage) => result.Error switch
    {
        VoteError.None when result.Value is not null => Ok(result.Value),
        VoteError.NotFound => NotFound(new { error = notFoundMessage }),
        _ => Problem("An unexpected error occurred.")
    };

    /// <summary>Reads the current user's id from the JWT 'sub' / NameIdentifier claim.</summary>
    private bool TryGetUserId(out int userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out userId);
    }
}

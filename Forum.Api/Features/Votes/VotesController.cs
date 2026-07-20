// Forum.Api/Features/Votes/VotesController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forum.Api.Features.Votes;

[ApiController]
[Tags("Votes")]
public sealed class VotesController(IVoteService voteService) : ControllerBase
{
    // ===== Thread votes =====

    /// <summary>Get a thread's score and, if authenticated, the caller's own vote.</summary>
    [HttpGet("api/threads/{threadId:int}/vote")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(VoteTallyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetThreadTally(int threadId, CancellationToken ct)
    {
        var result = await voteService.GetThreadTallyAsync(threadId, TryGetUserId(out var uid) ? uid : null, ct);
        return MapResult(result, "Thread not found.");
    }

    /// <summary>Up/down-vote a thread (value +1 or -1). Sending the same value again clears it.</summary>
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

    /// <summary>Remove the caller's vote on a thread.</summary>
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

    /// <summary>Get a reply's score and, if authenticated, the caller's own vote.</summary>
    [HttpGet("api/posts/{postId:int}/vote")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(VoteTallyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPostTally(int postId, CancellationToken ct)
    {
        var result = await voteService.GetPostTallyAsync(postId, TryGetUserId(out var uid) ? uid : null, ct);
        return MapResult(result, "Reply not found.");
    }

    /// <summary>Up/down-vote a reply (value +1 or -1). Sending the same value again clears it.</summary>
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

    /// <summary>Remove the caller's vote on a reply.</summary>
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

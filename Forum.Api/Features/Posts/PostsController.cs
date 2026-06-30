// Forum.Api/Features/Posts/PostsController.cs
using System.Security.Claims;
using Forum.Api.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forum.Api.Features.Posts;

[ApiController]
public sealed class PostsController(IPostService postService) : ControllerBase
{
    // --- Nested under the parent thread: list + create ---

    [HttpGet("api/threads/{threadId:int}/posts")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<PostResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListByThread(
        int threadId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await postService.ListByThreadAsync(threadId, page, pageSize, ct);
        if (result.Error == PostError.ThreadNotFound)
            return NotFound(new { error = "Thread not found." });

        Response.Headers["X-Total-Count"] = result.Value!.Total.ToString();
        return Ok(result.Value.Items);
    }

    [HttpPost("api/threads/{threadId:int}/posts")]
    [Authorize]
    [ProducesResponseType(typeof(PostResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int threadId, CreatePostRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await postService.CreateAsync(threadId, request, userId, ct);
        return result.Error switch
        {
            PostError.None when result.Value is not null =>
                CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value),
            PostError.ThreadNotFound => NotFound(new { error = "Thread not found." }),
            PostError.ThreadLocked => Conflict(new { error = "This thread is locked; new replies are disabled." }),
            _ => Problem("An unexpected error occurred.")
        };
    }

    // --- Addressed by their own global id: get + update + delete ---

    [HttpGet("api/posts/{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PostResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await postService.GetByIdAsync(id, ct);
        return MapResult(result);
    }

    [HttpPut("api/posts/{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(PostResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdatePostRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await postService.UpdateAsync(id, request, userId, IsModerator(), ct);
        return MapResult(result);
    }

    [HttpDelete("api/posts/{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await postService.DeleteAsync(id, userId, IsModerator(), ct);
        return result.Error switch
        {
            PostError.None => NoContent(),
            PostError.NotFound => NotFound(new { error = "Reply not found." }),
            PostError.Forbidden => Forbid(),
            _ => Problem("An unexpected error occurred.")
        };
    }

    private IActionResult MapResult(PostResult<PostResponse> result) => result.Error switch
    {
        PostError.None when result.Value is not null => Ok(result.Value),
        PostError.NotFound => NotFound(new { error = "Reply not found." }),
        PostError.Forbidden => Forbid(),
        PostError.ThreadLocked => Conflict(new { error = "This thread is locked; replies cannot be edited." }),
        _ => Problem("An unexpected error occurred.")
    };

    /// <summary>Reads the current user's id from the JWT 'sub' / NameIdentifier claim.</summary>
    private bool TryGetUserId(out int userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out userId);
    }

    /// <summary>True if the caller carries the moderator role claim.</summary>
    private bool IsModerator() => User.IsInRole(ModeratorRole.Name);
}

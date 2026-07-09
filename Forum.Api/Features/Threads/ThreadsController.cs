// Forum.Api/Features/Threads/ThreadsController.cs
using System.Security.Claims;
using Forum.Api.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forum.Api.Features.Threads;

[ApiController]
[Route("api/threads")]
public sealed class ThreadsController(IThreadService threadService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<ThreadSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? categoryId = null,
        [FromQuery] string? tag = null,
        CancellationToken ct = default)
    {
        var items = await threadService.ListAsync(page, pageSize, categoryId, tag, ct);
        var total = await threadService.CountAsync(categoryId, tag, ct);
        Response.Headers["X-Total-Count"] = total.ToString();
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ThreadDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await threadService.GetByIdAsync(id, ct);
        return MapResult(result);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ThreadDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(CreateThreadRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await threadService.CreateAsync(request, userId, ct);
        if (result.Succeeded && result.Value is not null)
            return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);

        return MapResult(result);
    }

    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ThreadDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateThreadRequest request, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await threadService.UpdateAsync(id, request, userId, ct);
        return MapResult(result);
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await threadService.DeleteAsync(id, userId, IsModerator(), ct);
        return result.Error switch
        {
            ThreadError.None => NoContent(),
            ThreadError.NotFound => NotFound(new { error = "Thread not found." }),
            ThreadError.Forbidden => Forbid(),
            _ => Problem("An unexpected error occurred.")
        };
    }

    private IActionResult MapResult(ThreadResult<ThreadDetailResponse> result) => result.Error switch
    {
        ThreadError.None when result.Value is not null => Ok(result.Value),
        ThreadError.NotFound => NotFound(new { error = "Thread not found." }),
        ThreadError.Forbidden => Forbid(),
        ThreadError.CategoryNotFound => BadRequest(new { error = "Category not found." }),
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

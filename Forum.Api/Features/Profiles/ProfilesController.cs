// Forum.Api/Features/Profiles/ProfilesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forum.Api.Features.Profiles;

[ApiController]
[Route("api/users")]
public sealed class ProfilesController(IProfileService profileService) : ControllerBase
{
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(int id, CancellationToken ct)
    {
        var result = await profileService.GetProfileAsync(id, ct);
        return result.Error switch
        {
            ProfileError.None when result.Value is not null => Ok(result.Value),
            ProfileError.NotFound => NotFound(new { error = "User not found." }),
            _ => Problem("An unexpected error occurred.")
        };
    }

    [HttpGet("{id:int}/threads")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<ProfileThreadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetThreads(
        int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await profileService.ListThreadsAsync(id, page, pageSize, ct);
        if (result.Error == ProfileError.NotFound)
            return NotFound(new { error = "User not found." });

        Response.Headers["X-Total-Count"] = result.Value!.Total.ToString();
        return Ok(result.Value.Items);
    }

    [HttpGet("{id:int}/posts")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<ProfilePostResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPosts(
        int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await profileService.ListPostsAsync(id, page, pageSize, ct);
        if (result.Error == ProfileError.NotFound)
            return NotFound(new { error = "User not found." });

        Response.Headers["X-Total-Count"] = result.Value!.Total.ToString();
        return Ok(result.Value.Items);
    }
}

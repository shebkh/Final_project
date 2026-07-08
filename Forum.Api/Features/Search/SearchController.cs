// Forum.Api/Features/Search/SearchController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forum.Api.Features.Search;

[ApiController]
[Route("api/search")]
public sealed class SearchController(ISearchService searchService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromQuery] int? categoryId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await searchService.SearchAsync(q, categoryId, page, pageSize, ct);
        return result.Error switch
        {
            SearchError.None when result.Value is not null => Ok(result.Value),
            SearchError.QueryTooShort => BadRequest(new { error = "Search term must be at least 2 characters." }),
            _ => Problem("An unexpected error occurred.")
        };
    }
}

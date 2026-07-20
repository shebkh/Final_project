// Forum.Api/Features/Categories/CategoriesController.cs
using Forum.Api.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forum.Api.Features.Categories;

[ApiController]
[Route("api/categories")]
[Tags("Categories")]
public sealed class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    /// <summary>List all categories (roots and their sub-categories) with thread counts.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var items = await categoryService.ListAsync(ct);
        return Ok(items);
    }

    /// <summary>Get a single category by id.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await categoryService.GetByIdAsync(id, ct);
        return MapResult(result);
    }

    /// <summary>Create a category or sub-category. Moderators only.</summary>
    [HttpPost]
    [Authorize(Roles = ModeratorRole.Name)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateCategoryRequest request, CancellationToken ct)
    {
        var result = await categoryService.CreateAsync(request, ct);
        if (result.Succeeded && result.Value is not null)
            return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);

        return MapResult(result);
    }

    /// <summary>Rename or re-parent a category. Moderators only.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = ModeratorRole.Name)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, UpdateCategoryRequest request, CancellationToken ct)
    {
        var result = await categoryService.UpdateAsync(id, request, ct);
        return MapResult(result);
    }

    /// <summary>Delete a category (threads become uncategorized). Moderators only.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = ModeratorRole.Name)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await categoryService.DeleteAsync(id, ct);
        return result.Error switch
        {
            CategoryError.None => NoContent(),
            CategoryError.NotFound => NotFound(new { error = "Category not found." }),
            CategoryError.HasChildren => Conflict(new { error = "Delete or move its sub-categories first." }),
            _ => Problem("An unexpected error occurred.")
        };
    }

    private IActionResult MapResult(CategoryResult<CategoryResponse> result) => result.Error switch
    {
        CategoryError.None when result.Value is not null => Ok(result.Value),
        CategoryError.NotFound => NotFound(new { error = "Category not found." }),
        CategoryError.ParentNotFound => BadRequest(new { error = "Parent category not found." }),
        CategoryError.ParentIsChild => BadRequest(new { error = "Sub-categories cannot have children of their own." }),
        CategoryError.HasChildren => Conflict(new { error = "A category with sub-categories must stay a root category." }),
        CategoryError.NameTaken => Conflict(new { error = "A category with this name already exists." }),
        _ => Problem("An unexpected error occurred.")
    };
}

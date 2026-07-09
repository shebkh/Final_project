// Forum.Web/Features/Threads/Threads.razor.cs
using Microsoft.AspNetCore.Components;

namespace Forum.Web.Features.Threads;

public partial class Threads : ComponentBase
{
    /// <summary>Active category filter, round-tripped through the URL (?categoryId=).</summary>
    [SupplyParameterFromQuery(Name = "categoryId")]
    public int? CategoryId { get; set; }

    /// <summary>Active tag filter, round-tripped through the URL (?tag=).</summary>
    [SupplyParameterFromQuery(Name = "tag")]
    public string? Tag { get; set; }

    private IReadOnlyList<ThreadSummaryResponse> _threads = [];
    private bool _loading = true;
    private string? _error;

    // Sentinels distinct from any real filter (including null) so the first
    // OnParametersSetAsync always loads.
    private int? _loadedCategoryId = int.MinValue;
    private string? _loadedTag = "\0";

    protected override async Task OnParametersSetAsync()
    {
        // Fires on first render and again whenever the query string changes
        // (same-page navigations from the filter, category badges, or tag chips).
        if (_loadedCategoryId == CategoryId && _loadedTag == Tag)
            return;

        _loadedCategoryId = CategoryId;
        _loadedTag = Tag;
        _loading = true;

        var outcome = await ThreadApi.ListAsync(categoryId: CategoryId, tag: Tag);
        if (outcome.Succeeded)
        {
            _threads = outcome.Data!;
            _error = null;
        }
        else
        {
            _error = outcome.Error;
        }
        _loading = false;
    }

    private void OnFilterChangedAsync(int? categoryId)
    {
        // Changing the category keeps an active tag filter, and vice versa.
        var query = new List<string>(2);
        if (categoryId is not null) query.Add($"categoryId={categoryId}");
        if (!string.IsNullOrWhiteSpace(Tag)) query.Add($"tag={Uri.EscapeDataString(Tag)}");

        Navigation.NavigateTo(query.Count == 0 ? "/threads" : $"/threads?{string.Join('&', query)}");
    }
}

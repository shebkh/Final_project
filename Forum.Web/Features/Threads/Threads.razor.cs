// Forum.Web/Features/Threads/Threads.razor.cs
using Microsoft.AspNetCore.Components;

namespace Forum.Web.Features.Threads;

public partial class Threads : ComponentBase
{
    /// <summary>Active category filter, round-tripped through the URL (?categoryId=).</summary>
    [SupplyParameterFromQuery(Name = "categoryId")]
    public int? CategoryId { get; set; }

    private IReadOnlyList<ThreadSummaryResponse> _threads = [];
    private bool _loading = true;
    private string? _error;

    // Sentinel distinct from any real filter (including null) so the first
    // OnParametersSetAsync always loads.
    private int? _loadedCategoryId = int.MinValue;

    protected override async Task OnParametersSetAsync()
    {
        // Fires on first render and again whenever the query string changes
        // (same-page navigations from the filter or category badges).
        if (_loadedCategoryId == CategoryId)
            return;

        _loadedCategoryId = CategoryId;
        _loading = true;

        var outcome = await ThreadApi.ListAsync(categoryId: CategoryId);
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

    private void OnFilterChangedAsync(int? categoryId) =>
        Navigation.NavigateTo(categoryId is null ? "/threads" : $"/threads?categoryId={categoryId}");
}

// Forum.Web/Features/Search/Search.razor.cs
using Microsoft.AspNetCore.Components;

namespace Forum.Web.Features.Search;

public partial class Search : ComponentBase
{
    /// <summary>Active search term, round-tripped through the URL (?q=).</summary>
    [SupplyParameterFromQuery(Name = "q")]
    public string? Q { get; set; }

    /// <summary>Active category filter, round-tripped through the URL (?categoryId=).</summary>
    [SupplyParameterFromQuery(Name = "categoryId")]
    public int? CategoryId { get; set; }

    // Form state (what the user is typing) — synced from the URL on load so
    // shared/bookmarked links repopulate the controls.
    private string _input = string.Empty;
    private int? _filterCategoryId;

    private IReadOnlyList<SearchResultResponse> _results = [];
    private int _total;
    private bool _loading;
    private bool _searched;
    private string? _error;

    // Sentinels distinct from any real values so the first OnParametersSetAsync
    // always evaluates the URL state.
    private string? _loadedQ = "\0";
    private int? _loadedCategoryId = int.MinValue;

    protected override async Task OnParametersSetAsync()
    {
        // Fires on first render and on same-page navigations (form submits,
        // category badge clicks) that only change the query string.
        if (_loadedQ == Q && _loadedCategoryId == CategoryId)
            return;

        _loadedQ = Q;
        _loadedCategoryId = CategoryId;
        _input = Q ?? string.Empty;
        _filterCategoryId = CategoryId;

        if (string.IsNullOrWhiteSpace(Q))
        {
            // Bare /search — prompt state, no API call.
            _searched = false;
            _results = [];
            _error = null;
            return;
        }

        _loading = true;
        _error = null;

        var outcome = await SearchApi.SearchAsync(Q, CategoryId);
        if (outcome.Succeeded)
        {
            _results = outcome.Data!.Items;
            _total = outcome.Data.Total;
            _searched = true;
        }
        else
        {
            _error = outcome.Error;
        }
        _loading = false;
    }

    private void SubmitAsync()
    {
        var q = _input.Trim();
        if (q.Length == 0)
            return;

        var url = $"/search?q={Uri.EscapeDataString(q)}";
        if (_filterCategoryId is not null)
            url += $"&categoryId={_filterCategoryId}";

        Navigation.NavigateTo(url);
    }
}

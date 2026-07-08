// Forum.Web/Features/Categories/Categories.razor.cs
using Microsoft.AspNetCore.Components;

namespace Forum.Web.Features.Categories;

public partial class Categories : ComponentBase
{
    private IReadOnlyList<CategoryResponse> _categories = [];
    private bool _loading = true;
    private string? _error;

    private readonly CategoryEditModel _createModel = new();
    private bool _createBusy;
    private string? _createError;

    private int? _editingId;
    private CategoryEditModel _editModel = new();
    private bool _editBusy;
    private string? _editError;

    private int? _deleteBusyId;
    private int? _actionErrorId;
    private string? _actionError;

    private IEnumerable<CategoryResponse> Roots =>
        _categories.Where(c => c.ParentId is null);

    private IEnumerable<CategoryResponse> ChildrenOf(int parentId) =>
        _categories.Where(c => c.ParentId == parentId);

    protected override Task OnInitializedAsync() => ReloadAsync();

    private async Task ReloadAsync()
    {
        var outcome = await CategoryApi.ListAsync();
        if (outcome.Succeeded)
        {
            _categories = outcome.Data!;
            _error = null;
        }
        else
        {
            _error = outcome.Error;
        }
        _loading = false;
    }

    private async Task CreateAsync()
    {
        _createBusy = true;
        _createError = null;

        var outcome = await CategoryApi.CreateAsync(_createModel);
        if (outcome.Succeeded)
        {
            _createModel.Name = string.Empty;
            _createModel.ParentId = null;
            await ReloadAsync();
        }
        else
        {
            _createError = outcome.Error;
        }
        _createBusy = false;
    }

    private void BeginEdit(CategoryResponse category)
    {
        _editingId = category.Id;
        _editModel = new CategoryEditModel { Name = category.Name, ParentId = category.ParentId };
        _editError = null;
        _actionError = null;
    }

    private void CancelEdit()
    {
        _editingId = null;
        _editError = null;
    }

    private async Task SaveEditAsync()
    {
        if (_editingId is null) return;
        _editBusy = true;
        _editError = null;

        var outcome = await CategoryApi.UpdateAsync(_editingId.Value, _editModel);
        if (outcome.Succeeded)
        {
            _editingId = null;
            await ReloadAsync();
        }
        else
        {
            _editError = outcome.Error;
        }
        _editBusy = false;
    }

    private async Task DeleteAsync(CategoryResponse category)
    {
        _deleteBusyId = category.Id;
        _actionErrorId = null;
        _actionError = null;

        var outcome = await CategoryApi.DeleteAsync(category.Id);
        if (outcome.Succeeded)
        {
            await ReloadAsync();
        }
        else
        {
            _actionErrorId = category.Id;
            _actionError = outcome.Error;
        }
        _deleteBusyId = null;
    }
}

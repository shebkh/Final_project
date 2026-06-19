// Forum.Web/Features/Posts/PostList.razor.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Forum.Web.Features.Posts;

public partial class PostList : ComponentBase
{
    [Parameter, EditorRequired] public int ThreadId { get; set; }
    [CascadingParameter] private Task<AuthenticationState>? AuthState { get; set; }

    [Inject] private IPostApiClient PostApi { get; set; } = default!;

    private readonly List<PostResponse> _posts = [];
    private readonly PostEditModel _newModel = new();
    private readonly PostEditModel _editModel = new();

    private int _count;
    private int? _currentUserId;
    private int? _editingId;
    // Row-scoped busy: the id of the post whose edit/delete is in flight (null = none).
    private int? _busyPostId;
    private bool _loading = true;
    private bool _addBusy;
    private string? _loadError;    // initial-load failure only (replaces the whole list)
    private string? _addError;     // inline on the add-reply form
    private string? _editError;    // inline on the edit form
    private string? _actionError;  // inline; per-row delete failure — never hides the list

    protected override async Task OnInitializedAsync()
    {
        _currentUserId = await CurrentUserIdAsync();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var outcome = await PostApi.ListByThreadAsync(ThreadId);
        if (outcome.Succeeded)
        {
            _posts.Clear();
            _posts.AddRange(outcome.Data!);
            _count = _posts.Count;
            _loadError = null;
        }
        else
        {
            _loadError = outcome.Error;
        }
        _loading = false;
    }

    private async Task AddReplyAsync()
    {
        _addBusy = true;
        _addError = null;

        var outcome = await PostApi.CreateAsync(ThreadId, _newModel);
        if (outcome.Succeeded)
        {
            _posts.Add(outcome.Data!);
            _count = _posts.Count;
            _newModel.Body = string.Empty;
        }
        else
        {
            _addError = outcome.Error;
        }
        _addBusy = false;
    }

    private void BeginEdit(PostResponse post)
    {
        _editingId = post.Id;
        _editModel.Body = post.Body;
        _editError = null;
    }

    private void CancelEdit()
    {
        _editingId = null;
        _editError = null;
    }

    private async Task SaveEditAsync()
    {
        if (_editingId is not int id) return;

        _busyPostId = id;
        _editError = null;

        var outcome = await PostApi.UpdateAsync(id, _editModel);
        if (outcome.Succeeded)
        {
            var index = _posts.FindIndex(p => p.Id == id);
            if (index >= 0)
                _posts[index] = outcome.Data!;
            _editingId = null;
        }
        else
        {
            _editError = outcome.Error;
        }
        _busyPostId = null;
    }

    private async Task DeleteAsync(int id)
    {
        _busyPostId = id;
        _actionError = null;

        var outcome = await PostApi.DeleteAsync(id);
        if (outcome.Succeeded)
        {
            _posts.RemoveAll(p => p.Id == id);
            _count = _posts.Count;
        }
        else
        {
            // Per-action error, rendered inline — must NOT touch _loadError,
            // which would collapse the whole list and the add-reply form.
            _actionError = outcome.Error;
        }
        _busyPostId = null;
    }

    private bool IsRowBusy(int postId) => _busyPostId == postId;

    private async Task<int?> CurrentUserIdAsync()
    {
        if (AuthState is null) return null;
        var user = (await AuthState).User;
        var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(raw, out var id) ? id : null;
    }
}

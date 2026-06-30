// Forum.Web/Features/Threads/ThreadDetail.razor.cs
using System.Security.Claims;
using Forum.Web.Features.Moderation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Forum.Web.Features.Threads;

public partial class ThreadDetail : ComponentBase
{
    [Parameter] public int Id { get; set; }
    [CascadingParameter] private Task<AuthenticationState>? AuthState { get; set; }

    private ThreadDetailResponse? _thread;
    private bool _loading = true;
    private bool _isOwner;
    private bool _isModerator;
    private bool _deleting;
    private bool _modBusy;
    private string? _error;
    private string? _actionError;
    private string? _modError;

    protected override async Task OnInitializedAsync()
    {
        var outcome = await ThreadApi.GetByIdAsync(Id);
        if (outcome.Succeeded)
        {
            _thread = outcome.Data;
            var user = await CurrentUserAsync();
            _isOwner = CurrentUserId(user) == _thread!.AuthorId;
            _isModerator = user?.IsInRole("Moderator") ?? false;
        }
        else
        {
            _error = outcome.Error;
        }
        _loading = false;
    }

    private async Task DeleteAsync()
    {
        _deleting = true;
        _actionError = null;

        var outcome = await ThreadApi.DeleteAsync(Id);
        if (outcome.Succeeded)
            Navigation.NavigateTo("/threads");
        else
        {
            _actionError = outcome.Error;
            _deleting = false;
        }
    }

    private async Task TogglePinAsync()
    {
        if (_thread is null) return;
        _modBusy = true;
        _modError = null;

        var outcome = await ModerationApi.SetPinnedAsync(Id, !_thread.IsPinned);
        if (outcome.Succeeded)
            _thread = _thread with { IsPinned = outcome.Data!.IsPinned, IsLocked = outcome.Data.IsLocked };
        else
            _modError = outcome.Error;

        _modBusy = false;
    }

    private async Task ToggleLockAsync()
    {
        if (_thread is null) return;
        _modBusy = true;
        _modError = null;

        var outcome = await ModerationApi.SetLockedAsync(Id, !_thread.IsLocked);
        if (outcome.Succeeded)
            _thread = _thread with { IsPinned = outcome.Data!.IsPinned, IsLocked = outcome.Data.IsLocked };
        else
            _modError = outcome.Error;

        _modBusy = false;
    }

    private async Task<ClaimsPrincipal?> CurrentUserAsync()
    {
        if (AuthState is null) return null;
        return (await AuthState).User;
    }

    private static int? CurrentUserId(ClaimsPrincipal? user)
    {
        var raw = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(raw, out var id) ? id : null;
    }
}

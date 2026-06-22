// Forum.Web/Features/Profiles/UserProfile.razor.cs
using Microsoft.AspNetCore.Components;

namespace Forum.Web.Features.Profiles;

public partial class UserProfile : ComponentBase
{
    private enum ProfileTab { Threads, Posts }

    [Parameter] public int Id { get; set; }

    [Inject] private IProfileApiClient ProfileApi { get; set; } = default!;

    private UserProfileResponse? _profile;
    private bool _loading = true;
    private string? _error;

    private ProfileTab _tab = ProfileTab.Threads;

    private IReadOnlyList<ProfileThreadResponse> _threads = [];
    private bool _threadsLoading;
    private bool _threadsLoaded;
    private string? _threadsError;

    private IReadOnlyList<ProfilePostResponse> _posts = [];
    private bool _postsLoading;
    private bool _postsLoaded;
    private string? _postsError;

    protected override async Task OnParametersSetAsync()
    {
        // Re-runs when navigating between /users/{id} for different ids.
        _loading = true;
        _error = null;
        _threadsLoaded = _postsLoaded = false;
        _threads = [];
        _posts = [];
        _tab = ProfileTab.Threads;

        var outcome = await ProfileApi.GetProfileAsync(Id);
        if (outcome.Succeeded)
        {
            _profile = outcome.Data;
            _loading = false;
            await LoadThreadsAsync();
        }
        else
        {
            _profile = null;
            _error = outcome.Error;
            _loading = false;
        }
    }

    private async Task ShowTab(ProfileTab tab)
    {
        _tab = tab;
        if (tab == ProfileTab.Threads)
            await LoadThreadsAsync();
        else
            await LoadPostsAsync();
    }

    private async Task LoadThreadsAsync()
    {
        if (_threadsLoaded) return;

        _threadsLoading = true;
        _threadsError = null;

        var outcome = await ProfileApi.ListThreadsAsync(Id);
        if (outcome.Succeeded)
        {
            _threads = outcome.Data!;
            _threadsLoaded = true;
        }
        else
        {
            _threadsError = outcome.Error;
        }

        _threadsLoading = false;
    }

    private async Task LoadPostsAsync()
    {
        if (_postsLoaded) return;

        _postsLoading = true;
        _postsError = null;

        var outcome = await ProfileApi.ListPostsAsync(Id);
        if (outcome.Succeeded)
        {
            _posts = outcome.Data!;
            _postsLoaded = true;
        }
        else
        {
            _postsError = outcome.Error;
        }

        _postsLoading = false;
    }
}

// Forum.Web/Features/Votes/VoteBox.razor.cs
using Microsoft.AspNetCore.Components;

namespace Forum.Web.Features.Votes;

public partial class VoteBox : ComponentBase
{
    [Parameter, EditorRequired] public VoteTargetKind Kind { get; set; }
    [Parameter, EditorRequired] public int TargetId { get; set; }

    [Inject] private IVoteApiClient VoteApi { get; set; } = default!;

    private VoteTallyResponse? _tally;
    private bool _busy;
    private string? _error;

    private string ScoreClass => (_tally?.Score ?? 0) switch
    {
        > 0 => "vote-score-pos",
        < 0 => "vote-score-neg",
        _ => string.Empty
    };

    protected override async Task OnInitializedAsync()
    {
        var outcome = await VoteApi.GetTallyAsync(Kind, TargetId);
        if (outcome.Succeeded)
            _tally = outcome.Data;
        else
            _error = outcome.Error;
    }

    private async Task CastAsync(int value)
    {
        _busy = true;
        _error = null;

        // Casting the same value again toggles it off — the API handles that and
        // returns the resulting tally either way.
        var outcome = await VoteApi.CastAsync(Kind, TargetId, value);
        if (outcome.Succeeded)
            _tally = outcome.Data;
        else
            _error = outcome.Error;

        _busy = false;
    }
}

// Forum.Web/Features/Notifications/SignalRService.cs
using Forum.Web.Features.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;

namespace Forum.Web.Features.Notifications;

public interface ISignalRService : IAsyncDisposable
{
    /// <summary>Raised for every notification pushed by the API. May fire on any thread.</summary>
    event Action<NotificationMessage>? NotificationReceived;

    /// <summary>
    /// Connects/disconnects to match the current authentication state and starts
    /// following sign-in/sign-out changes. Call once per circuit, after first
    /// interactive render (the token lives in localStorage → needs JS interop).
    /// </summary>
    Task EnsureStartedAsync();
}

/// <summary>
/// Scoped per Blazor Server circuit. Owns the HubConnection to the API's
/// notification hub: connects when the user is signed in (JWT sent as the
/// access token), disconnects on sign-out, auto-reconnects on transient drops.
/// Connection failures are logged and swallowed — notifications are an extra,
/// never a reason the forum stops working.
/// </summary>
public sealed class SignalRService(
    string hubUrl,
    ITokenStore tokenStore,
    AuthenticationStateProvider authStateProvider,
    ILogger<SignalRService> logger) : ISignalRService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private HubConnection? _connection;
    private bool _watchingAuth;

    public event Action<NotificationMessage>? NotificationReceived;

    public async Task EnsureStartedAsync()
    {
        if (!_watchingAuth)
        {
            _watchingAuth = true;
            authStateProvider.AuthenticationStateChanged += OnAuthStateChanged;
        }

        var state = await authStateProvider.GetAuthenticationStateAsync();
        await SyncToAuthAsync(state.User.Identity?.IsAuthenticated == true);
    }

    private void OnAuthStateChanged(Task<AuthenticationState> stateTask) =>
        _ = HandleAuthChangedAsync(stateTask);

    private async Task HandleAuthChangedAsync(Task<AuthenticationState> stateTask)
    {
        try
        {
            var state = await stateTask;
            await SyncToAuthAsync(state.User.Identity?.IsAuthenticated == true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to sync the notification connection to an auth change.");
        }
    }

    private async Task SyncToAuthAsync(bool isAuthenticated)
    {
        await _gate.WaitAsync();
        try
        {
            if (isAuthenticated)
                await ConnectAsync();
            else
                await DisconnectAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task ConnectAsync()
    {
        if (_connection is not null)
            return;

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = async () => await tokenStore.GetTokenAsync();
            })
            .WithAutomaticReconnect()
            .Build();

        connection.On<NotificationMessage>("Notify",
            message => NotificationReceived?.Invoke(message));

        try
        {
            await connection.StartAsync();
            _connection = connection;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not connect to the notification hub at {HubUrl}.", hubUrl);
            await connection.DisposeAsync();
        }
    }

    private async Task DisconnectAsync()
    {
        if (_connection is null)
            return;

        var connection = _connection;
        _connection = null;
        await connection.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_watchingAuth)
            authStateProvider.AuthenticationStateChanged -= OnAuthStateChanged;

        await _gate.WaitAsync();
        try
        {
            await DisconnectAsync();
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }
}

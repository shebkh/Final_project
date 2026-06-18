// Forum.Web/Features/Auth/TokenStore.cs
using Blazored.LocalStorage;

namespace Forum.Web.Features.Auth;

public interface ITokenStore
{
    /// <summary>The token cached for the current circuit, if loaded. Safe during prerender.</summary>
    string? CurrentToken { get; }

    Task<string?> GetTokenAsync();
    Task SetTokenAsync(string token);
    Task ClearTokenAsync();
}

/// <summary>
/// Scoped per Blazor Server circuit. Caches the JWT in memory so it can be read
/// synchronously (e.g. by the HTTP handler) and mirrors it to browser localStorage.
/// localStorage access is skipped gracefully when JS isn't ready (prerender).
/// </summary>
public sealed class TokenStore(ILocalStorageService localStorage) : ITokenStore
{
    private const string StorageKey = "forum_jwt";
    private string? _cached;
    private bool _loaded;

    public string? CurrentToken => _cached;

    public async Task<string?> GetTokenAsync()
    {
        if (_loaded)
            return _cached;

        try
        {
            _cached = await localStorage.GetItemAsStringAsync(StorageKey);
            _loaded = true;
        }
        catch (InvalidOperationException)
        {
            // JS interop unavailable (prerendering) — fall back to the in-memory value.
        }

        return _cached;
    }

    public async Task SetTokenAsync(string token)
    {
        _cached = token;
        _loaded = true;
        await localStorage.SetItemAsStringAsync(StorageKey, token);
    }

    public async Task ClearTokenAsync()
    {
        _cached = null;
        _loaded = true;
        await localStorage.RemoveItemAsync(StorageKey);
    }
}

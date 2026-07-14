using Cookies.Contract;

namespace QuakeSounds.Services;

// Thin typed wrapper around the real Cookies.Contract.IPlayerCookiesAPIv1 shared interface.
// Cookies auto-loads players on connect (OnClientPutInServer) and Set self-enqueues saves,
// so we only need Get/Has/Set here.
public sealed class PlayerCookiesApiWrapper
{
    private readonly IPlayerCookiesAPIv1 _api;

    public PlayerCookiesApiWrapper(IPlayerCookiesAPIv1 api)
    {
        _api = api;
    }

    public T? Get<T>(long steamId, string key) => _api.Get<T>(steamId, key);

    public bool Has(long steamId, string key) => _api.Has(steamId, key);

    public void Set<T>(long steamId, string key, T value) => _api.Set(steamId, key, value);

}

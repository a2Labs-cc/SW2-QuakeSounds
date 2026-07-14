using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using System;
using System.Collections.Concurrent;

namespace QuakeSounds.Services;

public class GameStateService
{
    private const string VolumeKey = "QuakeSounds.Volume";
    private const string EnabledKey = "QuakeSounds.Enabled";

    private readonly ISwiftlyCore _core;
    private PlayerCookiesApiWrapper? _cookies;
    private readonly ConcurrentDictionary<int, int> _killCounts = new();
    private readonly ConcurrentDictionary<int, (int Count, long LastKillTime)> _multiKillState = new();
    private readonly ConcurrentDictionary<ulong, float> _playerVolumeOverride = new();
    private readonly ConcurrentDictionary<ulong, bool> _playerEnabledOverride = new();
    private readonly ConcurrentDictionary<string, long> _recentDeathEvents = new();

    public bool FirstBloodDone { get; set; } = false;

    public GameStateService(ISwiftlyCore core, PlayerCookiesApiWrapper? cookies)
    {
        _core = core;
        _cookies = cookies;
    }

    // UseSharedInterface (where the Cookies API gets resolved) always runs after Load
    // (where GameStateService is constructed), so the cookies API must be attachable
    // after construction instead of only via the constructor.
    public void SetPlayerCookiesApi(PlayerCookiesApiWrapper? cookies)
    {
        _cookies = cookies;
    }

    public void ClearRoundState()
    {
        _multiKillState.Clear();
        FirstBloodDone = false;
    }

    public void ResetKillCounts()
    {
        _killCounts.Clear();
    }

    public void ResetAll()
    {
        _killCounts.Clear();
        _multiKillState.Clear();
        _playerVolumeOverride.Clear();
        _playerEnabledOverride.Clear();
        _recentDeathEvents.Clear();
        FirstBloodDone = false;
    }

    public int GetKillCount(int playerId) => _killCounts.TryGetValue(playerId, out var count) ? count : 0;
    
    public int IncrementKillCount(int playerId)
    {
        return _killCounts.AddOrUpdate(playerId, 1, (_, current) => current + 1);
    }

    public void ResetKillCount(int playerId)
    {
        _killCounts.AddOrUpdate(playerId, 0, (_, _) => 0);
    }

    // Returns (CurrentMultiKillCount, IsNewMultiKill)
    public (int Count, bool Valid) UpdateMultiKill(int playerId, float windowSeconds)
    {
        var windowMs = (long)(windowSeconds * 1000.0f);
        if (windowMs <= 0) return (0, false);

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var result = _multiKillState.AddOrUpdate(
            playerId,
            _ => (1, now),
            (_, state) =>
            {
                var (count, lastMs) = state;
                if (now - lastMs <= windowMs)
                {
                    return (count + 1, now);
                }
                return (1, now);
            }
        );

        return (result.Item1, result.Item1 > 1);
    }

    public void SetPlayerVolume(ulong steamId, float volume)
    {
        _playerVolumeOverride[steamId] = volume;

        if (_cookies == null) return;

        try
        {
            _cookies.Set((long)steamId, VolumeKey, volume);
        }
        catch (Exception ex)
        {
            _core.Logger.LogError(ex, "[QuakeSounds] Failed to persist volume for {SteamId}", steamId);
        }
    }

    public float GetPlayerVolume(ulong steamId)
    {
        if (_playerVolumeOverride.TryGetValue(steamId, out var vol))
        {
            return vol;
        }

        if (TryLoadCookieValue(steamId, VolumeKey, out float cookieVolume))
        {
            _playerVolumeOverride[steamId] = cookieVolume;
            return cookieVolume;
        }

        return -1f; // -1 indicates no override
    }

    public void SetPlayerEnabled(ulong steamId, bool enabled)
    {
        _playerEnabledOverride[steamId] = enabled;

        if (_cookies == null) return;

        try
        {
            _cookies.Set((long)steamId, EnabledKey, enabled);
        }
        catch (Exception ex)
        {
            _core.Logger.LogError(ex, "[QuakeSounds] Failed to persist enabled state for {SteamId}", steamId);
        }
    }

    public bool IsPlayerEnabled(ulong steamId)
    {
        if (_playerEnabledOverride.TryGetValue(steamId, out var enabled))
        {
            return enabled;
        }

        if (TryLoadCookieValue(steamId, EnabledKey, out bool cookieEnabled))
        {
            _playerEnabledOverride[steamId] = cookieEnabled;
            return cookieEnabled;
        }

        return true;
    }

    private bool TryLoadCookieValue<T>(ulong steamId, string key, out T value)
    {
        value = default!;
        if (_cookies == null) return false;

        try
        {
            if (!_cookies.Has((long)steamId, key)) return false;

            var cookieValue = _cookies.Get<T>((long)steamId, key);
            if (cookieValue == null) return false;

            value = cookieValue;
            return true;
        }
        catch (Exception ex)
        {
            _core.Logger.LogError(ex, "[QuakeSounds] Failed to load cookie '{Key}' for {SteamId}", key, steamId);
            return false;
        }
    }

    public bool ShouldProcessDeathEvent(ulong attackerSteamId, ulong victimSteamId, bool headshot, bool noScope, string weapon, long dedupeWindowMs)
    {
        if (dedupeWindowMs <= 0) return true;

        var weaponKey = weapon ?? string.Empty;
        var key = $"{attackerSteamId}:{victimSteamId}:{(headshot ? 1 : 0)}:{(noScope ? 1 : 0)}:{weaponKey}";
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        if (_recentDeathEvents.TryAdd(key, now))
        {
            return true;
        }

        if (_recentDeathEvents.TryGetValue(key, out var last) && now - last <= dedupeWindowMs)
        {
            _recentDeathEvents[key] = now;
            return false;
        }

        _recentDeathEvents[key] = now;
        return true;
    }
}

using System;
using System.Collections.Concurrent;

namespace QuakeSounds.Services;

public class GameStateService
{
    private readonly ConcurrentDictionary<int, int> _killCounts = new();
    private readonly ConcurrentDictionary<int, (int Count, long LastKillTime)> _multiKillState = new();
    private readonly ConcurrentDictionary<ulong, float> _playerVolumeOverride = new();
    private readonly ConcurrentDictionary<ulong, bool> _playerEnabledOverride = new();
    private readonly ConcurrentDictionary<string, long> _recentDeathEvents = new();
    
    public bool FirstBloodDone { get; set; } = false;

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
    }

    public float GetPlayerVolume(ulong steamId)
    {
        return _playerVolumeOverride.TryGetValue(steamId, out var vol) ? vol : -1f; // -1 indicates no override
    }

    public void SetPlayerEnabled(ulong steamId, bool enabled)
    {
        _playerEnabledOverride[steamId] = enabled;
    }

    public bool IsPlayerEnabled(ulong steamId)
    {
        return _playerEnabledOverride.TryGetValue(steamId, out var enabled) ? enabled : true;
    }

    public bool ShouldProcessDeathEvent(ulong attackerSteamId, ulong victimSteamId, bool headshot, string weapon, long dedupeWindowMs)
    {
        if (dedupeWindowMs <= 0) return true;

        var weaponKey = weapon ?? string.Empty;
        var key = $"{attackerSteamId}:{victimSteamId}:{(headshot ? 1 : 0)}:{weaponKey}";
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

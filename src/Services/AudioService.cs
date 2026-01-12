using AudioApi;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuakeSounds.Services;

public class AudioService
{
    private readonly ISwiftlyCore _core;
    private readonly IAudioApi? _audioApi;
    private readonly ConcurrentDictionary<string, IAudioSource> _decodedSources = new();
    private int _channelCounter = 0;

    public AudioService(ISwiftlyCore core, IAudioApi? audioApi)
    {
        _core = core;
        _audioApi = audioApi;
    }

    public void ClearCache()
    {
        _decodedSources.Clear();
        _channelCounter = 0;
    }

    public void RemovePlayerChannel(int playerId)
    {
    }

    public bool TryPlay(IPlayer attacker, string soundKey, QuakeSounds.QuakeSoundsConfig config, Func<ulong, bool> isPlayerEnabled, Func<ulong, float> getPlayerVolume)
    {
        float GetEffectiveVolume(ulong steamId)
        {
            var volume = config.Volume;
            var overrideVolume = getPlayerVolume(steamId);
            if (overrideVolume >= 0) volume = overrideVolume;
            return Math.Clamp(volume, 0f, 1f);
        }

        if (_audioApi == null)
        {
            _core.Logger.LogWarning("[QuakeSounds] Audio API missing, cannot play sound.");
            return false;
        }

        if (!config.Sounds.TryGetValue(soundKey, out var configuredPath) || string.IsNullOrWhiteSpace(configuredPath))
        {
            _core.Logger.LogWarning("[QuakeSounds] Sound key '{Key}' is not mapped in config.", soundKey);
            return false;
        }

        var resolvedPath = ResolvePath(configuredPath);

        if (!File.Exists(resolvedPath))
        {
            _core.Logger.LogWarning("[QuakeSounds] Missing sound file for key '{Key}': {Path}", soundKey, resolvedPath);
            return false;
        }

        IAudioSource source;
        try
        {
            source = _decodedSources.GetOrAdd(resolvedPath, path => _audioApi.DecodeFromFile(path));
        }
        catch (Exception ex)
        {
            _core.Logger.LogError(ex, "[QuakeSounds] Failed to decode sound file: {Path}", resolvedPath);
            return false;
        }

        var channelId = $"quakesounds.{System.Threading.Interlocked.Increment(ref _channelCounter)}";
        var channel = _audioApi.UseChannel(channelId);
        channel.SetSource(source);

        if (config.PlayToAll)
        {
            var anyPlayed = false;
            foreach (var player in _core.PlayerManager.GetAllPlayers().Where(p => p is { IsValid: true } && !p.IsFakeClient))
            {
                if (!isPlayerEnabled(player.SteamID))
                {
                    continue;
                }

                var volume = GetEffectiveVolume(player.SteamID);
                channel.SetVolume(player.PlayerID, volume);
                channel.Play(player.PlayerID);
                anyPlayed = true;
            }
            return anyPlayed;
        }

        if (!isPlayerEnabled(attacker.SteamID))
        {
            return false;
        }

        var attackerVolume = GetEffectiveVolume(attacker.SteamID);
        channel.SetVolume(attacker.PlayerID, attackerVolume);
        channel.Play(attacker.PlayerID);
        return true;
    }

    private string ResolvePath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath)) return configuredPath;

        var dataPath = Path.Combine(_core.PluginDataDirectory, configuredPath);
        if (File.Exists(dataPath)) return dataPath;

        return Path.Combine(_core.PluginPath, configuredPath);
    }
}

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
    private readonly ConcurrentDictionary<int, IAudioChannelController> _playerChannels = new();

    public AudioService(ISwiftlyCore core, IAudioApi? audioApi)
    {
        _core = core;
        _audioApi = audioApi;
    }

    public void ClearCache()
    {
        _decodedSources.Clear();
        _playerChannels.Clear();
    }

    public bool TryPlay(IPlayer attacker, string soundKey, QuakeSounds.QuakeSoundsConfig config, Func<ulong, bool> isPlayerEnabled, Func<ulong, float> getPlayerVolume)
    {
        if (_audioApi == null) return false;

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

        var channel = _playerChannels.GetOrAdd(attacker.PlayerID, id => _audioApi.UseChannel($"quakesounds.{id}"));
        channel.SetSource(source);

        if (config.PlayToAll)
        {
            var anyPlayed = false;
            foreach (var player in _core.PlayerManager.GetAllPlayers().Where(p => p is { IsValid: true } && !p.IsFakeClient))
            {
                if (!isPlayerEnabled(player.SteamID)) continue;

                var volume = config.Volume;
                var overrideVolume = getPlayerVolume(player.SteamID);
                if (overrideVolume >= 0) volume = overrideVolume;

                channel.SetVolume(player.PlayerID, Math.Clamp(volume, 0f, 1f));
                channel.Play(player.PlayerID);
                anyPlayed = true;
            }
            return anyPlayed;
        }

        if (!isPlayerEnabled(attacker.SteamID)) return false;

        var attackerVolume = config.Volume;
        var attackerOverride = getPlayerVolume(attacker.SteamID);
        if (attackerOverride >= 0) attackerVolume = attackerOverride;

        channel.SetVolume(attacker.PlayerID, Math.Clamp(attackerVolume, 0f, 1f));
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

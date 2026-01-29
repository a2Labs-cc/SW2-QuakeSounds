using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace QuakeSounds.Services;

public class AudioService : ISoundService
{
    private readonly ISwiftlyCore _core;
    private readonly dynamic _audioApi;
    private readonly ConcurrentDictionary<string, object> _decodedSources = new();
    private int _channelCounter = 0;

    private AudioService(ISwiftlyCore core, dynamic audioApi)
    {
        _core = core;
        _audioApi = audioApi;
    }

    public static ISoundService Create(ISwiftlyCore core, object audioApi)
    {
        return new AudioService(core, audioApi);
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
            if (config.Debug)
            {
                _core.Logger.LogWarning("[QuakeSounds] Sound key '{Key}' is not mapped in config.", soundKey);
            }
            return false;
        }

        var resolvedPath = ResolvePath(configuredPath);

        if (config.Debug)
        {
            _core.Logger.LogInformation("[QuakeSounds] TryPlay soundKey={Key} configuredPath={ConfiguredPath} resolvedPath={ResolvedPath} playToAll={PlayToAll}", soundKey, configuredPath, resolvedPath, config.PlayToAll);
        }

        if (!File.Exists(resolvedPath))
        {
            return false;
        }

        object source;
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
        dynamic channel = _audioApi.UseChannel(channelId);

        try
        {
            // Avoid RuntimeBinderException by invoking SetSource with the actual runtime type.
            // This also makes AudioApi type mismatches (multiple assemblies) easier to diagnose.
            var setSource = ((object)channel).GetType().GetMethod("SetSource");
            if (setSource == null)
            {
                _core.Logger.LogWarning("[QuakeSounds] Audio channel does not have SetSource method.");
                return false;
            }

            setSource.Invoke(channel, new[] { source });
        }
        catch (Exception ex)
        {
            var channelType = ((object)channel).GetType();
            var sourceType = source?.GetType();
            _core.Logger.LogError(ex,
                "[QuakeSounds] Failed to SetSource on audio channel. ChannelType={ChannelType} SourceType={SourceType} SourceAssembly={SourceAssembly}",
                channelType.FullName,
                sourceType?.FullName ?? "NULL",
                sourceType?.Assembly.FullName ?? "NULL");
            return false;
        }

        if (config.PlayToAll)
        {
            var anyPlayed = false;
            foreach (var player in _core.PlayerManager.GetAllPlayers().Where(p => p is { IsValid: true } && !p.IsFakeClient))
            {
                if (!isPlayerEnabled(player.SteamID))
                {
                    continue;
                }

                if (player.PlayerID <= 0)
                {
                    if (config.Debug)
                    {
                        _core.Logger.LogWarning("[QuakeSounds] Audio PlayToAll skipped due to invalid PlayerID. PlayerID={PlayerID} SteamID={SteamID}", player.PlayerID, player.SteamID);
                    }
                    continue;
                }

                var volume = GetEffectiveVolume(player.SteamID);

                try
                {
                    if (config.Debug)
                    {
                        _core.Logger.LogInformation("[QuakeSounds] Audio PlayToAll -> PlayerID={PlayerID} SteamID={SteamID} Volume={Volume}", player.PlayerID, player.SteamID, volume);
                    }

                    channel.SetVolume(player.PlayerID, volume);
                    channel.Play(player.PlayerID);
                    anyPlayed = true;
                }
                catch (Exception ex)
                {
                    _core.Logger.LogError(ex, "[QuakeSounds] Audio failed for player. PlayerID={PlayerID} SteamID={SteamID} soundKey={Key}", player.PlayerID, player.SteamID, soundKey);
                }
            }
            return anyPlayed;
        }

        if (!isPlayerEnabled(attacker.SteamID))
        {
            return false;
        }

        var attackerVolume = GetEffectiveVolume(attacker.SteamID);
        try
        {
            if (config.Debug)
            {
                _core.Logger.LogInformation("[QuakeSounds] Audio Play -> PlayerID={PlayerID} SteamID={SteamID} Volume={Volume} soundKey={Key}", attacker.PlayerID, attacker.SteamID, attackerVolume, soundKey);
            }

            if (attacker.PlayerID <= 0)
            {
                _core.Logger.LogWarning("[QuakeSounds] Audio Play skipped due to invalid PlayerID. PlayerID={PlayerID} SteamID={SteamID} soundKey={Key}", attacker.PlayerID, attacker.SteamID, soundKey);
                return false;
            }

            channel.SetVolume(attacker.PlayerID, attackerVolume);
            channel.Play(attacker.PlayerID);
            return true;
        }
        catch (Exception ex)
        {
            _core.Logger.LogError(ex, "[QuakeSounds] Audio failed for attacker. PlayerID={PlayerID} SteamID={SteamID} soundKey={Key}", attacker.PlayerID, attacker.SteamID, soundKey);
            return false;
        }
    }

    private string ResolvePath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath)) return configuredPath;

        var dataPath = Path.Combine(_core.PluginDataDirectory, configuredPath);
        if (File.Exists(dataPath)) return dataPath;

        return Path.Combine(_core.PluginPath, configuredPath);
    }
}

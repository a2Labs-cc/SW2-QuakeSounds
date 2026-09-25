using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Sounds;
using System;
using System.Linq;
using Volume.Contract;

namespace QuakeSounds.Services;

public class AddonSoundService : ISoundService
{
    private readonly ISwiftlyCore _core;
    private IPlayerVolumeAPI? _volumeApi;

    public AddonSoundService(ISwiftlyCore core)
    {
        _core = core;
    }

    public void SetVolumeApi(IPlayerVolumeAPI? volumeApi) => _volumeApi = volumeApi;

    public void ClearCache()
    {
    }

    public bool TryPlay(IPlayer attacker, string soundKey, QuakeSounds.QuakeSoundsConfig config, Func<ulong, bool> isPlayerEnabled)
    {
        float GetEffectiveVolume(ulong steamId)
        {
            if (_volumeApi == null) return Math.Clamp(config.Volume, 0f, 1f);
            try
            {
                return Math.Clamp(_volumeApi.GetEffectiveVolume((long)steamId, "QuakeSounds"), 0f, 1f);
            }
            catch (Exception ex)
            {
                _core.Logger.LogError(ex, "[QuakeSounds] Volume API failed for {SteamId}", steamId);
                return Math.Clamp(config.Volume, 0f, 1f);
            }
        }

        if (!config.Sounds.TryGetValue(soundKey, out var soundPath) || string.IsNullOrWhiteSpace(soundPath))
        {
            if (config.Debug)
            {
                _core.Logger.LogWarning("[QuakeSounds] Sound key '{Key}' is not mapped in config.", soundKey);
            }
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

                PlaySoundToPlayer(player, soundPath, GetEffectiveVolume(player.SteamID));
                anyPlayed = true;
            }
            return anyPlayed;
        }

        if (!isPlayerEnabled(attacker.SteamID))
        {
            return false;
        }

        PlaySoundToPlayer(attacker, soundPath, GetEffectiveVolume(attacker.SteamID));
        return true;
    }

    private void PlaySoundToPlayer(IPlayer player, string soundPath, float volume)
    {
        var sourceEntityIndex = -1;
        if (player.Pawn == null)
        {
            _core.Logger.LogWarning("[QuakeSounds] Player pawn is null, emitting sound without a source entity. PlayerID={PlayerID} SteamID={SteamID}", player.PlayerID, player.SteamID);
        }
        else
        {
            sourceEntityIndex = (int)player.Pawn.Index;
        }

        var soundName = soundPath.Replace(".vsnd_c", "").Replace(".vsnd", "");

        try
        {
            using var soundEvent = new SoundEvent
            {
                Name = soundName,
                Volume = volume,
                SourceEntityIndex = sourceEntityIndex
            };

            soundEvent.Recipients.AddRecipient(player.PlayerID);
            soundEvent.Emit();
        }
        catch (Exception ex)
        {
            _core.Logger.LogError(ex, "[QuakeSounds] Failed to emit sound '{Sound}'", soundName);
        }
    }
}

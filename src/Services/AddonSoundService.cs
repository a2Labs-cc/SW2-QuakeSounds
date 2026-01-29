using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Sounds;
using System;
using System.Linq;

namespace QuakeSounds.Services;

public class AddonSoundService : ISoundService
{
    private readonly ISwiftlyCore _core;

    public AddonSoundService(ISwiftlyCore core)
    {
        _core = core;
    }

    public void ClearCache()
    {
    }

    public bool TryPlay(IPlayer attacker, string soundKey, QuakeSounds.QuakeSoundsConfig config, Func<ulong, bool> isPlayerEnabled, Func<ulong, float> getPlayerVolume)
    {
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

                var volume = config.Volume;
                var overrideVolume = getPlayerVolume(player.SteamID);
                if (overrideVolume >= 0) volume = overrideVolume;

                PlaySoundToPlayer(player, soundPath, Math.Clamp(volume, 0f, 1f));
                anyPlayed = true;
            }
            return anyPlayed;
        }

        if (!isPlayerEnabled(attacker.SteamID))
        {
            return false;
        }

        var attackerVolume = config.Volume;
        var attackerOverrideVolume = getPlayerVolume(attacker.SteamID);
        if (attackerOverrideVolume >= 0) attackerVolume = attackerOverrideVolume;

        PlaySoundToPlayer(attacker, soundPath, Math.Clamp(attackerVolume, 0f, 1f));
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

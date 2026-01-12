using SwiftlyS2.Shared.GameEvents;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace QuakeSounds;

public partial class QuakeSounds
{
    [GameEventHandler(HookMode.Post)]
    public HookResult OnPlayerDeath(EventPlayerDeath @event)
    {
        if (!_config.Enabled)
        {
            return HookResult.Continue;
        }

        if (!_config.PlayInWarmup && (Core.EntitySystem.GetGameRules()?.WarmupPeriod ?? false))
        {
            return HookResult.Continue;
        }

        var victimId = @event.Accessor.GetInt32("userid");
        var attackerId = @event.Accessor.GetInt32("attacker");

        var victim = victimId > 0 ? Core.PlayerManager.GetPlayer(victimId) : null;
        if (victim is { IsValid: true } && _config.ResetKillsOnDeath)
        {
            _gameStateService.ResetKillCount(victim.PlayerID);
        }

        var attacker = attackerId > 0 ? Core.PlayerManager.GetPlayer(attackerId) : null;
        if (attacker is not { IsValid: true } || attacker.IsFakeClient)
        {
            return HookResult.Continue;
        }

        if (victim is { IsValid: true } && victim.PlayerID == attacker.PlayerID && !_config.CountSelfKills)
        {
            return HookResult.Continue;
        }

        if (victim is { IsValid: true } && victim.PlayerID != attacker.PlayerID && !_config.CountTeamKills)
        {
            try
            {
                if (victim.Controller.TeamNum == attacker.Controller.TeamNum)
                {
                    return HookResult.Continue;
                }
            }
            catch
            {
                // If team lookup fails for any reason, continue as a normal kill.
            }
        }

        var killCount = _gameStateService.IncrementKillCount(attacker.PlayerID);
        if (_config.Debug)
        {
            Core.Logger.LogInformation($"[QuakeSounds] Kill: {killCount} | Attacker: {attacker.Controller?.PlayerName ?? "Unknown"} | Headshot: {@event.Headshot}");
        }

        var (multiKillCount, isMultiKill) = _gameStateService.UpdateMultiKill(attacker.PlayerID, _config.MultiKillWindowSeconds);

        if (!_gameStateService.FirstBloodDone && victim is { IsValid: true } && victim.PlayerID != attacker.PlayerID)
        {
            _gameStateService.FirstBloodDone = true;
            if (TryPlay(attacker, "firstblood"))
            {
                return HookResult.Continue;
            }
        }

        if (isMultiKill)
        {
            if (_config.Debug)
            {
                Core.Logger.LogInformation($"[QuakeSounds] MultiKill detected: {multiKillCount}");
            }
            if (TryPlayKillStreak(attacker, multiKillCount))
            {
                if (_config.Debug)
                {
                    Core.Logger.LogInformation($"[QuakeSounds] MultiKill {multiKillCount} played");
                }
                return HookResult.Continue;
            }
            else
            {
                if (_config.Debug)
                {
                    Core.Logger.LogInformation($"[QuakeSounds] MultiKill {multiKillCount} NOT played (Missing key/sound)");
                }
            }
        }

        if (TryPlayKillStreak(attacker, killCount))
        {
            if (_config.Debug)
            {
                Core.Logger.LogInformation($"[QuakeSounds] KillStreak {killCount} played");
            }
            return HookResult.Continue;
        }
        else
        {
            if (_config.Debug)
            {
                bool hasKey = _config.KillStreakAnnounces.ContainsKey(killCount);
                Core.Logger.LogInformation($"[QuakeSounds] KillStreak {killCount} SKIPPED. Config has key: {hasKey}");
                if (hasKey)
                {
                    var soundKey = _config.KillStreakAnnounces[killCount];
                    bool hasSound = _config.Sounds.ContainsKey(soundKey);
                    Core.Logger.LogInformation($"[QuakeSounds] SoundKey: {soundKey} | In Sounds map: {hasSound}");
                }
            }
        }

        if (@event.Headshot)
        {
            bool playedHeadshot = TryPlay(attacker, "headshot");
            if (_config.Debug)
            {
                Core.Logger.LogInformation($"[QuakeSounds] Headshot played: {playedHeadshot}");
            }
            if (playedHeadshot) return HookResult.Continue;
        }

        if (@event.Weapon.Contains("knife", StringComparison.OrdinalIgnoreCase) && TryPlay(attacker, "humiliation"))
        {
            return HookResult.Continue;
        }

        if (@event.Weapon.Contains("hegrenade", StringComparison.OrdinalIgnoreCase) && TryPlay(attacker, "perfect"))
        {
            return HookResult.Continue;
        }

        if (victim is { IsValid: true } && victim.PlayerID == attacker.PlayerID && TryPlay(attacker, "perfect"))
        {
            return HookResult.Continue;
        }

        var weaponKey = NormalizeWeaponKey(@event.Weapon);
        if (TryPlay(attacker, weaponKey))
        {
            return HookResult.Continue;
        }

        return HookResult.Continue;
    }

    private bool TryPlay(IPlayer attacker, string soundKey)
    {
        // Try to play sound
        var played = _audioService?.TryPlay(
          attacker,
          soundKey,
          _config,
          id => _gameStateService.IsPlayerEnabled(id),
          id => _gameStateService.GetPlayerVolume(id)
        ) ?? false;

        if (_config.Sounds.ContainsKey(soundKey))
        {
            if (_config.PlayToAll)
            {
                var players = Core.PlayerManager.GetAllPlayers()
                    .Where(p => p is { IsValid: true } && !p.IsFakeClient)
                    .Where(p => _gameStateService.IsPlayerEnabled(p.SteamID));

                _messageService.PrintMessageToAll(players, attacker, soundKey, _config);
            }
            else
            {
                if (_gameStateService.IsPlayerEnabled(attacker.SteamID))
                {
                    _messageService.PrintMessage(attacker, attacker, soundKey, _config);
                }
            }
        }

        return played;
    }

    private bool TryPlayKillStreak(IPlayer attacker, int killCount)
    {
        if (_config.KillStreakAnnounces.TryGetValue(killCount, out var soundKey))
        {
            if (_config.Sounds.ContainsKey(soundKey))
            {
                TryPlay(attacker, soundKey);
                return true;
            }
        }
        return false;
    }

    private static string NormalizeWeaponKey(string weapon)
    {
        if (string.IsNullOrWhiteSpace(weapon))
        {
            return "weapon_unknown";
        }

        return weapon.StartsWith("weapon_", StringComparison.OrdinalIgnoreCase)
          ? weapon.ToLowerInvariant()
          : $"weapon_{weapon.ToLowerInvariant()}";
    }
}

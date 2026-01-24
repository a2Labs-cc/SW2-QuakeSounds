using SwiftlyS2.Shared.GameEvents;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using System.Linq;

namespace QuakeSounds;

public partial class QuakeSounds
{
  private void TryPlayRoundSoundToAll(string soundKey)
  {
    if (!IsPluginEnabled())
    {
      return;
    }

    if (IsWarmupBlockedByConfig())
    {
      return;
    }

    var anyPlayer = Core.PlayerManager.GetAllPlayers()
      .FirstOrDefault(p => p is { IsValid: true } && !p.IsFakeClient);

    if (anyPlayer == null)
    {
      return;
    }

    var originalPlayToAll = _config.PlayToAll;
    var originalCenter = _config.EnableCenterMessage;
    var originalChat = _config.EnableChatMessage;

    try
    {
      _config.PlayToAll = true;
      _config.EnableCenterMessage = false;
      _config.EnableChatMessage = false;

      _soundService?.TryPlay(
        anyPlayer,
        soundKey,
        _config,
        id => _gameStateService.IsPlayerEnabled(id),
        id => _gameStateService.GetPlayerVolume(id)
      );
    }
    finally
    {
      _config.PlayToAll = originalPlayToAll;
      _config.EnableCenterMessage = originalCenter;
      _config.EnableChatMessage = originalChat;
    }
  }

  [GameEventHandler(HookMode.Post)]
  public HookResult OnRoundStart(EventRoundStart @event)
  {
    if (_config.ResetKillsOnRoundStart)
    {
      _gameStateService.ResetKillCounts();
    }

    _gameStateService.ClearRoundState();
    TryPlayRoundSoundToAll("round_start");
    return HookResult.Continue;
  }

  [GameEventHandler(HookMode.Post)]
  public HookResult OnRoundFreezeEnd(EventRoundFreezeEnd @event)
  {
    TryPlayRoundSoundToAll("round_freeze_end");
    return HookResult.Continue;
  }
}

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

    var anyPlayer = Core.PlayerManager.GetAllPlayers()
      .FirstOrDefault(p => p is { IsValid: true } && !p.IsFakeClient);

    if (anyPlayer == null)
    {
      return;
    }

    var originalPlayToAll = _config.PlayToAll;
    var originalCenter = _config.Messages.EnableCenterMessage;
    var originalChat = _config.Messages.EnableChatMessage;

    try
    {
      _config.PlayToAll = true;
      _config.Messages.EnableCenterMessage = false;
      _config.Messages.EnableChatMessage = false;

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
      _config.Messages.EnableCenterMessage = originalCenter;
      _config.Messages.EnableChatMessage = originalChat;
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

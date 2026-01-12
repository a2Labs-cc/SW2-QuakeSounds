using SwiftlyS2.Shared.GameEvents;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;

namespace QuakeSounds;

public partial class QuakeSounds
{
  [GameEventHandler(HookMode.Post)]
  public HookResult OnRoundStart(EventRoundStart @event)
  {
    if (_config.ResetKillsOnRoundStart)
    {
      _gameStateService.ResetKillCounts();
    }

    _gameStateService.ClearRoundState();
    return HookResult.Continue;
  }
}

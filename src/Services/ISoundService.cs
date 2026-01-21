using SwiftlyS2.Shared.Players;
using System;

namespace QuakeSounds.Services;

public interface ISoundService
{
    bool TryPlay(IPlayer attacker, string soundKey, QuakeSounds.QuakeSoundsConfig config, Func<ulong, bool> isPlayerEnabled, Func<ulong, float> getPlayerVolume);
    void ClearCache();
}

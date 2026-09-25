using SwiftlyS2.Shared.Players;
using Volume.Contract;
using System;

namespace QuakeSounds.Services;

public interface ISoundService
{
    bool TryPlay(IPlayer attacker, string soundKey, QuakeSounds.QuakeSoundsConfig config, Func<ulong, bool> isPlayerEnabled);
    void SetVolumeApi(IPlayerVolumeAPI? volumeApi);
    void ClearCache();
}

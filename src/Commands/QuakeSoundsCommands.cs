using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;

namespace QuakeSounds;

public partial class QuakeSounds
{
    private string Localize(string key)
    {
        return Core.Localizer[key] ?? string.Empty;
    }

    [Command("quake")]
    public void QuakeCommand(ICommandContext context)
    {
        Core.Logger.LogInformation("[QuakeSounds] QuakeCommand called by {Player}", context.Sender?.Controller?.PlayerName ?? "Unknown");
        var sender = context.Sender!;

        var currentState = _gameStateService.IsPlayerEnabled(sender.SteamID);
        var newState = !currentState;
        _gameStateService.SetPlayerEnabled(sender.SteamID, newState);

        var enabledMsg = Localize("commands.quake.enabled");
        var disabledMsg = Localize("commands.quake.disabled");
        Core.Logger.LogInformation("[QuakeSounds] QuakeCommand replying with: {State}", newState ? "enabled" : "disabled");
        context.Reply(newState ? enabledMsg : disabledMsg);
    }
}

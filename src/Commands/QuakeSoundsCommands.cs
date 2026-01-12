using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;
using System;

namespace QuakeSounds;

public partial class QuakeSounds
{
    private string Localize(string key)
    {
        return Core.Localizer[key] ?? string.Empty;
    }

    private string FormatWith(string template, params (string Key, string Value)[] tokens)
    {
        var result = template;
        foreach (var (key, value) in tokens)
        {
            result = result.Replace($"{{{key}}}", value);
        }
        return result;
    }

    [Command("volume")]
    public void VolumeCommand(ICommandContext context)
    {
        var sender = context.Sender!;

        if (context.Args.Length < 1)
        {
            var storedVolume = _gameStateService.GetPlayerVolume(sender.SteamID);
            var effectiveVolume = storedVolume >= 0 ? storedVolume : _config.Volume;
            var displayVolume = Math.Clamp((int)Math.Round(effectiveVolume * 10), 0, 10);

            var currentMsg = Localize("commands.volume.current");
            var usageMsg = Localize("commands.volume.usage");

            context.Reply(FormatWith(currentMsg, ("volume", displayVolume.ToString())) + "\n" + usageMsg);
            return;
        }

        if (!int.TryParse(context.Args[0], out var volInt))
        {
            return;
        }

        if (volInt < 0) volInt = 0;
        if (volInt > 10) volInt = 10;

        var vol = volInt / 10.0f;
        _gameStateService.SetPlayerVolume(sender.SteamID, vol);

        var setMsg = Localize("commands.volume.set");
        context.Reply(FormatWith(setMsg, ("volume", volInt.ToString())));
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

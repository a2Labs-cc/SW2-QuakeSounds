using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using System.Collections.Generic;

namespace QuakeSounds.Services;

public class MessageService
{
    private readonly ISwiftlyCore _core;

    public MessageService(ISwiftlyCore core)
    {
        _core = core;
    }

    public void PrintMessage(IPlayer recipient, IPlayer attacker, string soundKey, global::QuakeSounds.QuakeSounds.QuakeSoundsConfig config)
    {
        if (!config.EnableChatMessage && !config.EnableCenterMessage) return;

        // Get the "inner" message (e.g. "Double Kill")
        var messageKey = $"quake.{soundKey.ToLower()}";
        var messageContent = _core.Localizer[messageKey];
        
        // Fallback to soundKey if translation missing
        if (string.IsNullOrEmpty(messageContent) || messageContent == messageKey)
        {
             messageContent = soundKey;
        }

        // Determine format
        bool isSelf = recipient.PlayerID == attacker.PlayerID;
        var formatKey = isSelf ? "quake.chat.player" : "quake.chat.other";
        var format = _core.Localizer[formatKey];
        
        if (string.IsNullOrEmpty(format) || format == formatKey) 
        {
            format = isSelf ? "You made a [message]" : "[player] made a [message]";
        }

        // Format
        var playerName = attacker.Controller?.PlayerName ?? "Unknown";
        var prefix = $"{config.ChatPrefixColor}{config.ChatPrefix}";
        var plainPrefix = $"{config.ChatPrefix}";
        
        var plainFormatted = format
            .Replace("[prefix]", plainPrefix)
            .Replace("[message]", messageContent)
            .Replace("[player]", playerName);

        var formatted = format
            .Replace("[prefix]", prefix)
            .Replace("[message]", messageContent)
            .Replace("[player]", playerName)
            .Colored();

        if (config.EnableChatMessage)
        {
            recipient.SendChat(formatted);
        }

        if (config.EnableCenterMessage)
        {
            recipient.SendAlert(messageContent);
        }
    }

    public void PrintMessageToAll(IEnumerable<IPlayer> recipients, IPlayer attacker, string soundKey, global::QuakeSounds.QuakeSounds.QuakeSoundsConfig config)
    {
        foreach (var recipient in recipients)
        {
            PrintMessage(recipient, attacker, soundKey, config);
        }
    }
}

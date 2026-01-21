using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QuakeSounds;

public partial class QuakeSounds
{
  public sealed class QuakeSoundsConfig
  {
    public bool Enabled { get; set; } = true;
    public bool Debug { get; set; } = false;
    public bool PlayToAll { get; set; } = false;
    public float Volume { get; set; } = 1.0f;
    public bool CountSelfKills { get; set; } = false;
    public bool CountTeamKills { get; set; } = false;
    public bool ResetKillsOnDeath { get; set; } = true;
    public bool ResetKillsOnRoundStart { get; set; } = true;

    public bool PlayInWarmup { get; set; } = false;

    public float MultiKillWindowSeconds { get; set; } = 1.5f;

    [JsonPropertyName("messages")]
    public MessageSettings Messages { get; set; } = new();

    public Dictionary<string, string> Sounds { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
      ["combowhore"] = "combowhore.mp3",
      ["dominating"] = "dominating.mp3",
      ["doublekill"] = "doublekill.mp3",
      ["firstblood"] = "firstblood.mp3",
      ["godlike"] = "godlike.mp3",
      ["headshot"] = "headshot.mp3",
      ["holyshit"] = "holyshit.mp3",
      ["humiliation"] = "humiliation.mp3",
      ["impressive"] = "impressive.mp3",
      ["knife_kill"] = "knife_kill.mp3",
      ["killingspree"] = "killingspree.mp3",
      ["ludicrouskill"] = "ludicrouskill.mp3",
      ["monsterkill"] = "monsterkill.mp3",
      ["multikill"] = "multikill.mp3",
      ["ownage"] = "ownage.mp3",
      ["perfect"] = "perfect.mp3",
      ["rampage"] = "rampage.mp3",
      ["round_freeze_end"] = "round_freeze_end.mp3",
      ["round_start"] = "round_start.mp3",
      ["taser_kill"] = "taser_kill.mp3",
      ["triplekill"] = "triplekill.mp3",
      ["ultrakill"] = "ultrakill.mp3",
      ["unstoppable"] = "unstoppable.mp3",
      ["wickedsick"] = "wickedsick.mp3",
      ["wrecker"] = "wrecker.mp3"
    };

    public Dictionary<int, string> KillStreakAnnounces { get; set; } = new()
    {
      [2] = "doublekill",
      [3] = "triplekill",
      [4] = "ultrakill",
      [5] = "multikill",
      [6] = "rampage",
      [7] = "killingspree",
      [8] = "dominating",
      [9] = "impressive",
      [10] = "unstoppable",
      [11] = "megakill",
      [12] = "wickedsick",
      [13] = "monsterkill",
      [14] = "ludicrouskill",
      [15] = "godlike",
      [16] = "wrecker",
      [18] = "holyshit",
      [20] = "ownage",
      [25] = "combowhore"
    };
  }

  public class MessageSettings
  {
    [JsonPropertyName("enable_center_message")]
    public bool EnableCenterMessage { get; set; } = true;

    [JsonPropertyName("enable_chat_message")]
    public bool EnableChatMessage { get; set; } = true;

    [JsonPropertyName("chat_prefix")]
    public string ChatPrefix { get; set; } = "[QuakeSounds]";

    [JsonPropertyName("chat_prefix_color")]
    public string ChatPrefixColor { get; set; } = "[green]";
  }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using QuakeSounds.Services;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Convars;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace QuakeSounds;

[PluginMetadata(Id = "QuakeSounds", Version = "1.2.2", Name = "QuakeSounds", Author = "aga", Description = "No description.")]
public partial class QuakeSounds : BasePlugin {
  private ISoundService? _soundService;
  private readonly GameStateService _gameStateService;
  private readonly MessageService _messageService;
  private QuakeSoundsConfig _config = new();
  private readonly HashSet<string> _missingSoundFilesLogged = new(StringComparer.OrdinalIgnoreCase);
  private IConVar<int>? _qsEnabled;
  private IConVar<int>? _mpWarmupPauseTimer;
  private IDisposable? _configReloadRegistration;
  private readonly List<string> _registeredCommands = new();

  public QuakeSounds(ISwiftlyCore core) : base(core)
  {
    _gameStateService = new GameStateService();
    _messageService = new MessageService(core);
  }

  public override void ConfigureSharedInterface(IInterfaceManager interfaceManager)
  {
  }

  public override void UseSharedInterface(IInterfaceManager interfaceManager)
  {
    if (_config.UseAudioPlugin)
    {
      if (!interfaceManager.HasSharedInterface("audio"))
      {
        Core.Logger.LogWarning("[QuakeSounds] Audio plugin not found, falling back to addon sounds mode.");
        _soundService = new AddonSoundService(Core);
        return;
      }

      try
      {
        var audioApiType = Type.GetType("AudioApi.IAudioApi, AudioApi");
        if (audioApiType == null)
        {
          Core.Logger.LogWarning("[QuakeSounds] AudioApi assembly not found, falling back to addon sounds mode.");
          _soundService = new AddonSoundService(Core);
          return;
        }

        var getSharedInterfaceMethod = typeof(IInterfaceManager).GetMethod("GetSharedInterface");
        var genericMethod = getSharedInterfaceMethod?.MakeGenericMethod(audioApiType);
        var audioApi = genericMethod?.Invoke(interfaceManager, new object[] { "audio" });

        if (audioApi == null)
        {
          Core.Logger.LogWarning("[QuakeSounds] Failed to get Audio API interface, falling back to addon sounds mode.");
          _soundService = new AddonSoundService(Core);
          return;
        }

        _soundService = AudioService.Create(Core, audioApi);
        Core.Logger.LogInformation("[QuakeSounds] Using Audio API mode.");
      }
      catch (Exception ex)
      {
        Core.Logger.LogError(ex, "[QuakeSounds] Failed to initialize Audio API, falling back to addon sounds mode.");
        _soundService = new AddonSoundService(Core);
      }
    }
    else
    {
      _soundService = new AddonSoundService(Core);
      Core.Logger.LogInformation("[QuakeSounds] Using addon sounds mode.");
    }
  }

  public override void Load(bool hotReload)
  {
    var configPath = Core.Configuration.GetConfigPath("config.jsonc");
    if (!File.Exists(configPath))
    {
      var defaults = QuakeSoundsConfig.CreateDefaults();
      var defaultJson = JsonSerializer.Serialize(defaults, new JsonSerializerOptions
      {
        WriteIndented = true
      });

      File.WriteAllText(configPath, defaultJson);
    }

    Core.Configuration.Configure(builder =>
    {
      builder.AddJsonFile("config.jsonc", optional: false, reloadOnChange: true);
    });

    ReloadConfig();

    _qsEnabled = Core.ConVar.CreateOrFind<int>(
      "qs_enabled",
      "Enable/disable QuakeSounds globally. 1 = enabled, 0 = disabled.",
      _config.Enabled ? 1 : 0,
      0,
      1
    );

    _mpWarmupPauseTimer = Core.ConVar.Find<int>("mp_warmup_pausetimer");

    if (!_config.UseAudioPlugin && !string.IsNullOrEmpty(_config.SoundEventFile))
    {
      Core.Event.OnPrecacheResource += Event_OnPrecacheResource;
    }

    _configReloadRegistration?.Dispose();
    _configReloadRegistration = ChangeToken.OnChange(
      () => Core.Configuration.Manager.GetReloadToken(),
      () =>
      {
        ReloadConfig();
        _soundService?.ClearCache();
      }
    );

    _registeredCommands.Add("volume");
    _registeredCommands.Add("quake");
    Core.Logger.LogInformation("[QuakeSounds] Plugin loaded successfully.");
  }

  public override void Unload()
  {
    if (!_config.UseAudioPlugin && !string.IsNullOrEmpty(_config.SoundEventFile))
    {
      Core.Event.OnPrecacheResource -= Event_OnPrecacheResource;
    }

    foreach (var commandName in _registeredCommands)
    {
      try
      {
        Core.Command.UnregisterCommand(commandName);
      }
      catch (Exception ex)
      {
        Core.Logger.LogError(ex, "[QuakeSounds] Failed to unregister command: {Command}", commandName);
      }
    }
    _registeredCommands.Clear();

    _configReloadRegistration?.Dispose();
    _configReloadRegistration = null;

    _soundService?.ClearCache();
    _gameStateService.ResetAll();
  }

  private void Event_OnPrecacheResource(SwiftlyS2.Shared.Events.IOnPrecacheResourceEvent @event)
  {
    if (!string.IsNullOrEmpty(_config.SoundEventFile))
    {
      @event.AddItem(_config.SoundEventFile);
    }
  }

  private void ReloadConfig()
  {
    var defaults = new QuakeSoundsConfig();
    QuakeSoundsConfig? loaded = Core.Configuration.Manager.Get<QuakeSoundsConfig>();
    loaded ??= Core.Configuration.Manager.GetSection("Main").Get<QuakeSoundsConfig>();

    _config = loaded ?? defaults;

    // Preserve the user's config exactly: if a sound key is missing (e.g. commented out in JSONC),
    // treat it as disabled by NOT re-populating it from defaults.
    // Ensure case-insensitive lookups regardless of how the configuration binder instantiated the dictionary.
    if (_config.Sounds == null)
    {
      _config.Sounds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
    else if (_config.Sounds.Comparer != StringComparer.OrdinalIgnoreCase)
    {
      _config.Sounds = new Dictionary<string, string>(_config.Sounds, StringComparer.OrdinalIgnoreCase);
    }

    _config.KillStreakAnnounces ??= new Dictionary<int, string>();
    foreach (var kvp in defaults.KillStreakAnnounces)
    {
      // Forcefully add missing keys from defaults
      if (!_config.KillStreakAnnounces.ContainsKey(kvp.Key))
      {
          _config.KillStreakAnnounces[kvp.Key] = kvp.Value;
      }
    }

    Core.Logger.LogInformation(
      "[QuakeSounds] Config reloaded. Enabled: {Enabled}. EnableChatMessage: {EnableChatMessage}, EnableCenterMessage: {EnableCenterMessage}. KillStreakAnnounces count: {Count}",
      _config.Enabled,
      _config.EnableChatMessage,
      _config.EnableCenterMessage,
      _config.KillStreakAnnounces.Count
    );

    if (_config.Debug && _config.UseAudioPlugin)
    {
      foreach (var kvp in _config.Sounds)
      {
        var key = kvp.Key;
        var configuredPath = kvp.Value;
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(configuredPath))
        {
          continue;
        }

        var resolvedPath = ResolveSoundPath(configuredPath);
        if (string.IsNullOrWhiteSpace(resolvedPath))
        {
          continue;
        }

        if (!File.Exists(resolvedPath) && _missingSoundFilesLogged.Add($"{key}|{resolvedPath}"))
        {
          Core.Logger.LogWarning("[QuakeSounds] Missing sound file for key '{Key}': {Path}", key, resolvedPath);
        }
      }
    }
  }

  private string ResolveSoundPath(string configuredPath)
  {
    if (Path.IsPathRooted(configuredPath)) return configuredPath;

    var dataPath = Path.Combine(Core.PluginDataDirectory, configuredPath);
    if (File.Exists(dataPath)) return dataPath;

    return Path.Combine(Core.PluginPath, configuredPath);
  }

  private bool IsWarmupBlockedByConfig()
  {
    if (_config.PlayInWarmup)
    {
      return false;
    }

    bool warmupActive = false;

    // Primary warmup detection via gamerules.
    // EntitySystem might not be ready early, so this is best-effort.
    try
    {
      var gameRules = Core.EntitySystem.GetGameRules();
      warmupActive = gameRules != null && gameRules.WarmupPeriod;
    }
    catch
    {
      // If game rules lookup fails for any reason, fall back to convar.
    }

    // Fallback warmup detection via game convar.
    // If the convar is missing, fail open (do not block sounds).
    if (!warmupActive)
    {
      warmupActive = (_mpWarmupPauseTimer?.Value ?? 0) != 0;
    }

    if (warmupActive && _config.Debug)
    {
      Core.Logger.LogInformation("[QuakeSounds] Blocking sound playback due to warmup (PlayInWarmup=false).");
    }

    return warmupActive;
  }

  private bool IsPluginEnabled()
  {
    return (_qsEnabled?.Value ?? (_config.Enabled ? 1 : 0)) != 0;
  }
}
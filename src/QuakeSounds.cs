using AudioApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using QuakeSounds.Services;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Plugins;
using System;
using System.Collections.Generic;

namespace QuakeSounds;

[PluginMetadata(Id = "QuakeSounds", Version = "1.0.2", Name = "QuakeSounds", Author = "aga", Description = "No description.")]
public partial class QuakeSounds : BasePlugin {
  private AudioService? _audioService;
  private readonly GameStateService _gameStateService;
  private readonly MessageService _messageService;
  private QuakeSoundsConfig _config = new();
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
    if (!interfaceManager.HasSharedInterface("audio"))
    {
      Core.Logger.LogWarning("[QuakeSounds] Audio shared interface not found. Install/enable the 'Audio' plugin.");
      _audioService = null;
      return;
    }

    var audioApi = interfaceManager.GetSharedInterface<IAudioApi>("audio");
    _audioService = new AudioService(Core, audioApi);
  }

  public override void Load(bool hotReload)
  {
    Core.Logger.LogInformation("[QuakeSounds] Load called - hotReload: {HotReload}, Instance: {InstanceHash}", hotReload, GetHashCode());
    
    Core.Configuration
      .InitializeJsonWithModel<QuakeSoundsConfig>("config.jsonc", "Main")
      .Configure(builder =>
      {
        builder.AddJsonFile("config.jsonc", optional: false, reloadOnChange: true);
      });

    ReloadConfig();

    _configReloadRegistration?.Dispose();
    _configReloadRegistration = ChangeToken.OnChange(
      () => Core.Configuration.Manager.GetReloadToken(),
      () =>
      {
        ReloadConfig();
        _audioService?.ClearCache();
      }
    );

    _registeredCommands.Add("volume");
    _registeredCommands.Add("quake");
    Core.Logger.LogInformation("[QuakeSounds] Load completed - Commands registered: {Count}", _registeredCommands.Count);
  }

  public override void Unload()
  {
    Core.Logger.LogInformation("[QuakeSounds] Unload called - Instance: {InstanceHash}", GetHashCode());
    
    foreach (var commandName in _registeredCommands)
    {
      try
      {
        Core.Logger.LogInformation("[QuakeSounds] Unregistering command: {Command}", commandName);

        Core.Command.UnregisterCommand(commandName);
      }
      catch (Exception ex)
      {
        Core.Logger.LogWarning(ex, "[QuakeSounds] Failed to unregister command: {Command}", commandName);
      }
    }
    _registeredCommands.Clear();

    _configReloadRegistration?.Dispose();
    _configReloadRegistration = null;

    _audioService?.ClearCache();
    _gameStateService.ResetAll();
    Core.Logger.LogInformation("[QuakeSounds] Unload completed");
  }

  private void ReloadConfig()
  {
    var defaults = new QuakeSoundsConfig();
    var loaded = Core.Configuration.Manager.GetSection("Main").Get<QuakeSoundsConfig>();
    _config = loaded ?? defaults;

    _config.Sounds ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var kvp in defaults.Sounds)
    {
      _config.Sounds.TryAdd(kvp.Key, kvp.Value);
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
    
    Core.Logger.LogInformation("[QuakeSounds] Config reloaded. Enabled: {Enabled}. KillStreakAnnounces count: {Count}", _config.Enabled, _config.KillStreakAnnounces.Count);
  }
}
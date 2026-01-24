<div align="center">

# [SwiftlyS2] QuakeSounds

<a href="https://github.com/a2Labs-cc/SW2-QuakeSounds/releases/latest">
  <img src="https://img.shields.io/github/v/release/a2Labs-cc/SW2-QuakeSounds?label=release&color=07f223&style=for-the-badge">
</a>
<a href="https://github.com/a2Labs-cc/SW2-QuakeSounds/issues">
  <img src="https://img.shields.io/github/issues/a2Labs-cc/SW2-QuakeSounds?label=issues&color=E63946&style=for-the-badge">
</a>
<a href="https://github.com/a2Labs-cc/SW2-QuakeSounds/releases">
  <img src="https://img.shields.io/github/downloads/a2Labs-cc/SW2-QuakeSounds/total?label=downloads&color=3A86FF&style=for-the-badge">
</a>
<a href="https://github.com/a2Labs-cc/SW2-QuakeSounds/stargazers">
  <img src="https://img.shields.io/github/stars/a2Labs-cc/SW2-QuakeSounds?label=stars&color=e3d322&style=for-the-badge">
</a>

<br/>
<sub>Made by <a href="https://github.com/agasking1337" target="_blank" rel="noopener noreferrer">aga</a></sub>

</div>


## Overview

**SwiftlyS2-QuakeSounds** is a SwiftlyS2 plugin that plays Quake-style announcer audio for kill streaks, multi-kills, first blood, and special weapon events.

It supports two playback modes:

- **Audio plugin mode**: Uses the Swiftly Audio plugin (MP3/WAV playback).
- **Workshop Addons**: Relies on addon sound events (.vsndevts).

## Support

Need help or have questions? Join our Discord server:

<p align="center">
  <a href="https://discord.gg/d853jMW2gh" target="_blank">
    <img src="https://img.shields.io/badge/Join%20Discord-5865F2?logo=discord&logoColor=white&style=for-the-badge" alt="Discord">
  </a>
</p>


## Download Shortcuts
<ul>
  <li>
    <code>📦</code>
    <strong>&nbsp;Download Latest Plugin Version</strong> &rarr;
    <a href="https://github.com/a2Labs-cc/SW2-QuakeSounds/releases/latest" target="_blank" rel="noopener noreferrer">Click Here</a>
  </li>
    <li>
    <code>⚙️</code>
    <strong>&nbsp;Download Latest Addons Manager</strong> &rarr;
    <a href="https://github.com/SwiftlyS2-Plugins/AddonsManager/releases/latest" target="_blank" rel="noopener noreferrer">Click Here</a>
  </li>
  <li>
    <code>⚙️</code>
    <strong>&nbsp;Download Latest SwiftlyS2 Version</strong> &rarr;
    <a href="https://github.com/swiftly-solution/swiftlys2/releases/latest" target="_blank" rel="noopener noreferrer">Click Here</a>
  </li>
</ul>

## Installation

<ul>
  <li>
    <code>⚠️</code>
    <strong>&nbsp;Swiftly Audio Plugin is optional</strong> &rarr;
    <a href="https://github.com/SwiftlyS2-Plugins/Audio/releases/latest" target="_blank" rel="noopener noreferrer">Click Here</a>
  </li>
</ul>

1. Download/build the plugin (publish output lands in `build/publish/QuakeSounds/`).
2. Copy the published plugin folder to your server:

```
.../game/csgo/addons/swiftlys2/plugins/QuakeSounds/
```
3. Ensure the `resources/` folder (translations, gamedata) and your audio files are alongside the DLL or in the plugin data directory paths referenced in `config.jsonc`.
4. Start/restart the server.

## Audio Files

<ul>
  <li>
    <code>💽</code>
    <strong>&nbsp;Download MP3 Files</strong> &rarr;
    <a href="https://github.com/agasking1337/QuakeSoundsFiles/releases/latest" target="_blank" rel="noopener noreferrer">Click Here</a>
  </li>
</ul>

1. Download the MP3 Files.
2. Copy the files into `swiftlys2/data/QuakeSounds`

## Configuration

The plugin uses SwiftlyS2's JSON config system.

- **File name**: `config.jsonc`
- **Location**: `game/csgo/addons/swiftlys2/configs/plugins/QuakeSounds/config.jsonc`

On first run the config is created automatically. The resolved path is logged on startup.

If you are using **Workshop Addons / sound events mode** (no Audio plugin), the default `Sounds` entries that reference `.mp3` files are only examples. You must replace them with the **actual sound event names** that exist in your `.vsndevts` and make sure `SoundEventFile` points to that `.vsndevts` so it gets precached.

### Key Configuration Options

- `Enabled`: Master on/off switch (default: true)
- `UseAudioPlugin`: Use the Swiftly Audio plugin if available; otherwise fall back to addon sounds mode (default: true)
- `SoundEventFile`: (Addon sounds mode) `.vsndevts` file to precache (default: `your_sound_events/quakesounds.vsndevts`)
- `PlayToAll`: Play sounds to all enabled players instead of only the killer (default: false)
- `Volume`: Base volume (0-1) used when no per-player override exists (default: 1.0)
- `CountSelfKills` / `CountTeamKills`: Whether to include suicides/team-kills in streaks (default: false / false)
- `ResetKillsOnDeath` / `ResetKillsOnRoundStart`: Reset counters after death or at round start (default: true / true)
- `PrioritizeSpecialKills`: When true, special kills (taser/knife/headshot/noscope) take priority over streak sounds (default: false)
- `PlayInWarmup`: Allow sounds during warmup (default: false)
- `MultiKillWindowSeconds`: Time window to chain multi-kills (default: 1.5)
- `EnableChatMessage` / `EnableCenterMessage`: Enable/disable chat and center-screen messages (default: true / true)
- `ChatPrefix` / `ChatPrefixColor`: Customize the chat prefix (default: `[QuakeSounds]` / `[green]`)
- `Sounds`: Map sound keys (e.g., `doublekill`, `headshot`, `weapon_awp`) to file names/paths
- `KillStreakAnnounces`: Map kill counts to sound keys used for streaks


#### Addon sounds mode

When using **Workshop Addons / sound events** mode, each `Sounds` entry should be a **sound event name** (e.g. `quakesounds.doublekill`).

### Commands

- `!volume <0-10>`: Set your personal QuakeSounds volume (falls back to `Volume` when unset).
- `!quake`: Toggle QuakeSounds on or off for yourself.

Per-player volume and enable/disable are stored in memory (reset on plugin unload / server restart).

### CVars

- `qs_enabled <0|1>`: Enable/disable QuakeSounds globally at runtime.

Examples:

```text
qs_enabled 0
qs_enabled 1
```

## Building

```bash
dotnet build
```

## Credits
- Original plugin [Kandru/cs2-quake-sounds](https://github.com/Kandru/cs2-quake-sounds)
- Readme template by [criskkky](https://github.com/criskkky)
- Release workflow based on [K4ryuu/K4-Guilds-SwiftlyS2 release workflow](https://github.com/K4ryuu/K4-Guilds-SwiftlyS2/blob/main/.github/workflows/release.yml)

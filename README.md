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

**SwiftlyS2-QuakeSounds** is a SwiftlyS2 plugin that plays Quake-style announcer audio for kill streaks, multi-kills, first blood, and special weapon events. It uses the shared Audio interface to play the sounds.

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
    <strong>&nbsp;Download Latest SwiftlyS2 Version</strong> &rarr;
    <a href="https://github.com/swiftly-solution/swiftlys2/releases/latest" target="_blank" rel="noopener noreferrer">Click Here</a>
  </li>
</ul>

## Installation

<ul>
  <li>
    <code>⚠️</code>
    <strong>&nbsp;Requires Swiftly Audio Plugin in order to work</strong> &rarr;
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
- **Section**: `swiftlys2/configs/plugins/QuakeSounds/`

On first run the config is created automatically. The resolved path is logged on startup.

### Key Configuration Options

- `Enabled`: Master on/off switch (default: true)
- `PlayToAll`: Play sounds to all enabled players instead of only the killer (default: false)
- `Volume`: Base volume (0-1) used when no per-player override exists (default: 1.0)
- `CountSelfKills` / `CountTeamKills`: Whether to include suicides/team-kills in streaks (default: false / false)
- `ResetKillsOnDeath` / `ResetKillsOnRoundStart`: Reset counters after death or at round start (default: true / true)
- `PlayInWarmup`: Allow sounds during warmup (default: false)
- `MultiKillWindowSeconds`: Time window to chain multi-kills (default: 1.5)
- `messages`: Enable chat/center messages and customize `chat_prefix` + `chat_prefix_color`
- `Sounds`: Map sound keys (e.g., `doublekill`, `headshot`, `weapon_awp`) to file names/paths
- `KillStreakAnnounces`: Map kill counts to sound keys used for streaks

### Commands

- `!volume <0-10>`: Set your personal QuakeSounds volume (falls back to `Volume` when unset).
- `!quake`: Toggle QuakeSounds on or off for yourself.

## Building

```bash
dotnet build
```

## Credits
- Original plugin [Kandru/cs2-quake-sounds](https://github.com/Kandru/cs2-quake-sounds)
- Readme template by [criskkky](https://github.com/criskkky)
- Release workflow based on [K4ryuu/K4-Guilds-SwiftlyS2 release workflow](https://github.com/K4ryuu/K4-Guilds-SwiftlyS2/blob/main/.github/workflows/release.yml)

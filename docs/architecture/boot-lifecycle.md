# Boot Lifecycle

## Purpose

This page traces startup from executable entrypoints into content initialization. Use it when changing boot defaults, dependency registration, prototype ignore rules, tile definitions, localization, UI theme defaults, or round setup hooks.

## Source Anchors

- `Content.Client/Program.cs`
- `Content.Server/Program.cs`
- `RobustToolbox/Robust.Client/ContentStart.cs`
- `RobustToolbox/Robust.Server/ContentStart.cs`
- `RobustToolbox/Robust.Shared/ContentPack/GameClient.cs`
- `RobustToolbox/Robust.Shared/ContentPack/GameServer.cs`
- `RobustToolbox/Robust.Shared/ContentPack/GameShared.cs`
- `Content.Client/Entry/EntryPoint.cs`
- `Content.Server/Entry/EntryPoint.cs`
- `Content.Shared/Entry/EntryPoint.cs`
- `Content.Client/IoC/ClientContentIoC.cs`
- `Content.Server/IoC/ServerContentIoC.cs`
- `Content.Shared/Module/SharedModuleTestingCallbacks.cs`

## Runtime Flow

The client and server executable `Program.cs` files are thin wrappers. Client calls `Robust.Client.ContentStart.Start(args)`. Server calls `Robust.Server.ContentStart.Start(args)`. RobustToolbox then loads the content pack and creates the content `EntryPoint` classes.

Content initialization is split into three layers:

- `Content.Shared.Entry.EntryPoint`: shared prototype ignore setup, tile definition registration, marking manager initialization, and debug network defaults.
- `Content.Client.Entry.EntryPoint`: client IoC registration, component auto-registration, client prototype ignore rules, UI/style/theme setup, input contexts, overlays, chat, preferences, title window, and default state selection.
- `Content.Server.Entry.EntryPoint`: server IoC registration, config preset loading, component auto-registration, prototype ignore rules, localization, database/preferences/admin/connection setup, game map manager, game ticker post-initialize, voting, Discord hooks, and server API.

Client startup eventually chooses a state:

- launcher connection flow when launched from the launcher
- replay loading when a bundled replay is detected
- `MainScreen` for normal standalone startup
- `GameplayState` after connecting or entering a game

Server startup loads config presets before most gameplay managers initialize. `Content.Server.Entry.EntryPoint.LoadConfigPresets` reads `/ConfigPresets/Build/debug.toml`, `/ConfigPresets/Build/development.toml`, and any configured presets from `/ConfigPresets/`.

## Customization Levers

- Default local server behavior: `config/server_config.toml` and `Resources/ConfigPresets/Build/*.toml`.
- Client default theme: `Content.Client/Entry/EntryPoint.cs` calls `SetDefaultTheme("SS14DefaultTheme")` and reads `CVars.InterfaceTheme`.
- Client HUD layout: `Content.Client/Gameplay/GameplayState.cs` reads `CCVars.UILayout` and loads a screen type.
- Input contexts: `Content.Client/Input/ContentContexts.cs`.
- Server map and preset defaults: `config/server_config.toml`, `Resources/Prototypes/game_presets.yml`, `Resources/Prototypes/Maps/`.
- Shared tile registration: `Content.Shared/Entry/EntryPoint.cs` and `Resources/Prototypes/Tiles/`.
- Prototype directories ignored at runtime: `Resources/IgnoredPrototypes/` and explicit `RegisterIgnore` calls in entrypoints.

## Fantasy Conversion Notes

Boot code should remain boring. For Mythos, most changes should be data or content-system changes. Use startup only for global defaults: new UI theme, new default map/preset, fantasy config preset, new tile definitions, or registering new managers.

Avoid hardcoding fantasy behavior in entrypoints unless the behavior truly applies to the whole fork. A "kingdom mode", "dungeon map pool", or "magic economy" should normally be expressed as prototypes, game presets, config vars, or systems.

## Agent Search Terms

```powershell
rg -n "ContentStart|Start\\(args\\)|EntryPoint" Content.Client Content.Server RobustToolbox
rg -n "PreInit\\(|Init\\(|PostInit\\(|LoadConfigPresets|SetDefaultTheme|SetupContexts" Content.Client Content.Server Content.Shared
rg -n "RegisterIgnore|IgnoreMissingComponents|DoAutoRegistrations|GenerateNetIds" Content.Client Content.Server Content.Shared
rg -n "ConfigPresets|defaultpreset|map =|lobbyenabled|round_restart_time" config Resources\\ConfigPresets
```


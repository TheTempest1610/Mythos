# Architecture Overview

## Purpose

This page maps the major project boundaries. SS14 is split into content projects and the RobustToolbox engine. Mythos should usually change content first, then engine code only when the existing engine abstractions are too narrow.

Official SS14 reference: https://docs.spacestation14.com/en/general-development/codebase-info/codebase-organization.html

## Source Anchors

- `SpaceStation14.slnx`: solution entry.
- `global.json`: pins the .NET SDK family.
- `Directory.Packages.props`: package version overrides for content projects.
- `MSBuild/Content.props`: common content project settings.
- `Content.Client/Content.Client.csproj`: client content executable.
- `Content.Server/Content.Server.csproj`: server content executable.
- `Content.Shared/Content.Shared.csproj`: shared content library.
- `Resources/manifest.yml`: content pack manifest.
- `RobustToolbox/Robust.Client/Robust.Client.csproj`: engine client.
- `RobustToolbox/Robust.Server/Robust.Server.csproj`: engine server.
- `RobustToolbox/Robust.Shared/Robust.Shared.csproj`: shared engine layer.
- `Content.Shared/_Mythos/`: fork-specific shared gameplay contracts.
- `Content.Server/_Mythos/`: fork-specific server gameplay.
- `Content.Client/_Mythos/`: fork-specific client presentation and predictive helpers.
- `Resources/Prototypes/_Mythos/`: fork prototypes and same-ID overrides.
- `Tools/_Mythos/`: fork import/validation tooling.

## Runtime Flow

The content projects are loaded as a content pack on top of RobustToolbox. The engine supplies the general runtime: ECS, networking, rendering, input, resource loading, UI framework, map serialization, physics, and content pack bootstrapping. The content projects supply SS14-specific behavior: rounds, stations, species, jobs, items, machines, atmospherics, combat rules, UI widgets, and prototype data.

`Content.Shared` is the contract layer. Shared components and systems define the network-visible or predicted behavior that both client and server need to understand. `Content.Server` is authoritative for most game state. `Content.Client` owns the local presentation layer and client-only helpers.

`Resources/` is not passive art storage. It is a major gameplay layer. Most entities, jobs, maps, recipes, alerts, themes, damage types, species, and many behavior parameters are defined by YAML prototypes and consumed by C# systems.

Mythos adds a parallel fork layer under `_Mythos` directories. New fantasy mechanics should enter there first. Existing examples:

- mana: `Content.Shared/_Mythos/Magic/Mana/`
- combat target and queue: `Content.Shared/_Mythos/Combat/`
- spell actions: `Resources/Prototypes/_Mythos/Actions/combat_spells.yml`
- fantasy tiles: `Resources/Prototypes/_Mythos/Tiles/_OV/`
- imported tile art: `Resources/Textures/Tiles/_Mythos/_OV/`
- variant-aware tile spawn UI: `Content.Client/_Mythos/TileSpawn/`

## Customization Levers

- Prefer `Resources/Prototypes/` for data-first changes such as new entities, jobs, species, maps, recipes, alerts, tiles, and UI themes.
- Prefer `Resources/Prototypes/_Mythos/` for Mythos prototypes and upstream prototype overrides.
- Prefer `Content.Shared/` when adding components, shared events, prediction-visible behavior, or new prototype classes.
- Prefer `Content.Server/` when the server owns authority, persistence, round flow, AI decisions, spawning, or rule enforcement.
- Prefer `Content.Client/` for HUD, menus, overlays, local input handling, sprites, effects, client-only prediction glue, and bound UI windows.
- Enter `RobustToolbox/` only when a feature needs an engine capability: serialization, map format, low-level UI, input framework, rendering, networking, or ECS behavior.
- Touch upstream SS14 content only when a hook or replacement must attach to an existing system. Tag every upstream edit with `// Mythos:`.

## Fantasy Conversion Notes

The safest conversion strategy is to keep the engine and ECS shape stable while replacing SS14's content layer. Most sci-fi identity lives in prototypes, assets, maps, localization, and domain systems such as power, atmos, radio, cargo, departments, and ID/access. Some systems can be renamed and reskinned. Others should be rebuilt behind the same component and UI patterns.

Expect these layers to change together:

- A player-facing concept usually has C# systems, YAML prototypes, textures, locale strings, guidebook entries, and UI controls.
- A major station system usually has map entities, machine prototypes, server systems, shared state, client UI, and localization.
- A role or species change usually touches jobs, loadouts, access, body prototypes, appearance, preferences, spawn logic, and UI.

## Mythos Rules of Thumb

- If the player must see it, predict it, or click it, start in `Content.Shared/_Mythos`.
- If the server must decide it, damage it, grant it, persist it, or spawn it, add a server system under `Content.Server/_Mythos`.
- If it is only presentation, local input, an overlay, or mapper UI, add it under `Content.Client/_Mythos`.
- If it is identity, balance, icon, map, role, tile, or item composition, prefer `Resources/Prototypes/_Mythos` and `Resources/Textures/_Mythos`.
- If it disables a whole SS14 mechanic during conversion, prefer a CVar gate plus small tagged upstream guards.

## Agent Search Terms

```powershell
rg --files Content.Client Content.Server Content.Shared Resources RobustToolbox
rg -n "class .*System|sealed partial class .*Component|\\[RegisterComponent\\]" Content.Client Content.Server Content.Shared
rg -n "\\[Prototype|type: .*" Content.Shared Content.Server Resources\\Prototypes -g "*.cs" -g "*.yml"
rg -n "ContentStart|GameClient|GameServer|GameShared|EntryPoint" Content.Client Content.Server Content.Shared RobustToolbox
rg -n "Mythos|_Mythos|// Mythos:" Content.Client Content.Server Content.Shared Resources Tools
```


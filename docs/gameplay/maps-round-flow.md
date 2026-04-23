# Maps and Round Flow

## Purpose

This page maps how maps are selected, loaded, converted into stations, and used by the round lifecycle. Use it when replacing space stations with fantasy maps, changing round start behavior, adding dungeon/settlement map pools, or replacing SS14 game modes.

## Source Anchors

- `Resources/Maps/`
- `Resources/Prototypes/Maps/`
- `Resources/Prototypes/Maps/Pools/default.yml`
- `Resources/Prototypes/game_presets.yml`
- `Resources/Prototypes/GameRules/`
- `Content.Shared/Maps/GameMapPrototype.cs`
- `Content.Shared/Maps/GameMapPrototype.MapSelection.cs`
- `Content.Server/Maps/GameMapManager.cs`
- `Content.Server/Maps/GameMapPoolPrototype.cs`
- `Content.Server/GameTicking/GameTicker.cs`
- `Content.Server/GameTicking/GameTicker.RoundFlow.cs`
- `Content.Server/GameTicking/GameTicker.Spawning.cs`
- `Content.Server/GameTicking/Presets/GamePresetPrototype.cs`
- `Content.Server/GameTicking/Rules/`
- `Content.Server/Station/Systems/StationSystem.cs`
- `Content.Server/Station/Systems/StationSpawningSystem.cs`
- `RobustToolbox/docs/Map Format.md`
- `RobustToolbox/Schemas/mapfile.yml`

## Runtime Flow

Server config chooses the initial game preset and map. `config/server_config.toml` contains values such as `defaultpreset` and `map`. Game presets come from `Resources/Prototypes/game_presets.yml`. Game maps come from `Resources/Prototypes/Maps/*.yml`.

A `gameMap` prototype names a map, points to a serialized map file, and defines station configs. Example fields include `mapName`, `mapPath`, `minPlayers`, and `stations`. A `gameMapPool` prototype lists eligible map IDs for rotation and voting.

`GameMapManager` watches map-related CVars, selects configured or random maps, tracks map rotation memory, checks player count eligibility, and exposes the selected `GameMapPrototype`.

The actual map file in `Resources/Maps/` is a YAML serialized save. It contains metadata, tilemap IDs, grids, and entities. Map entities can include station markers and `BecomesStation` components. During round setup, station systems convert map entities into station entities, assign jobs, spawn players, and initialize station data.

`GameTicker` owns the broad round lifecycle: lobby, preset selection, game rule startup, map loading, spawning, round start, round end, restart, and status shell integration. The class is split into partial files by behavior.

## Customization Levers

- Default local dev map: `config/server_config.toml`.
- Available map rotation: `Resources/Prototypes/Maps/Pools/*.yml`.
- Map metadata and station config: `Resources/Prototypes/Maps/*.yml`.
- Physical layouts: `Resources/Maps/*.yml` and map editor workflows.
- Round rules and presets: `Resources/Prototypes/game_presets.yml` and `Resources/Prototypes/GameRules/`.
- Player spawning and jobs: station spawning systems plus job prototypes.
- Station identity: station prototypes under `Resources/Prototypes/Entities/Stations/`.

## Fantasy Conversion Notes

The map layer is where "space station" becomes "world". Keep the map prototype pipeline, but replace station concepts deliberately:

- A station can become a town, keep, guild hall, dungeon hub, caravan, ship, or realm instance.
- Map pools can become overworld, dungeon, arena, tutorial, or event pools.
- Game presets can become adventure modes, survival modes, court intrigue, dungeon crawl, or sandbox modes.
- Station jobs and access can become guild roles, noble houses, classes, permissions, keys, or magical wards.

Do not remove station infrastructure until replacement ownership is clear. Many systems ask for a station entity to find jobs, records, announcements, alerts, events, and spawning context. A fantasy "settlement" can keep station components internally while the player-facing names and systems are replaced.

## Agent Search Terms

```powershell
rg -n "type: gameMap|type: gameMapPool|mapPath:|stations:|availableJobs:" Resources\\Prototypes\\Maps -g "*.yml"
rg -n "defaultpreset|map =|GameMap|GameMapPool|GameMapManager" config Content.Server Content.Shared Resources\\Prototypes
rg -n "class GameTicker|RoundFlow|Spawning|GamePreset|GameRule" Content.Server\\GameTicking
rg -n "BecomesStation|StationNameSetup|StationJobs|StationSpawning|StationConfig" Content.Server Content.Shared Resources\\Prototypes
rg -n "meta:|tilemap:|entities:|grids:" Resources\\Maps\\testspawn.yml
```


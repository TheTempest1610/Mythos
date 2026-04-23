# Mythos Audit Docs

This folder is a first-pass architecture atlas for converting Space Station 14 from hard sci-fi into the Mythos fantasy fork. It is written for AI agents first: every page favors source paths, symbols, prototype names, and search terms over polished public-facing prose.

The docs do not change gameplay behavior. They are a context database for future implementation work.

## How to Use This Atlas

- Start with [Architecture Overview](architecture/overview.md) to understand the project boundaries.
- Use [Boot Lifecycle](architecture/boot-lifecycle.md) when changing startup, config, dependency registration, or round initialization.
- Use [ECS and Prototypes](architecture/ecs-prototypes.md) before editing components, systems, events, or YAML prototypes.
- Use [Resources and Data](architecture/resources-data.md) before editing maps, textures, audio, localization, or config presets.
- Use [HUD, UI, and Themes](ui/hud-ui-theme.md) and [Input and Controls](ui/input-controls.md) for player-facing reskins and control changes.
- Use [Maps and Round Flow](gameplay/maps-round-flow.md), [Player Entities and Roles](gameplay/player-entities-roles.md), and [Interactions, Combat, and NPCs](gameplay/interactions-combat-npc.md) for gameplay conversion work.
- Use [Fantasy Change Levers](conversion/fantasy-change-levers.md) when deciding whether to reskin, reconfigure, extend, or remove an SS14 subsystem.
- Use [Agent Search Index](agent-search-index.md) for exact `rg` searches.

## Repository Shape

The actual repository root is this directory's parent:

- `Content.Client/`: client-only game code, UI, rendering hooks, input registration, and client systems.
- `Content.Server/`: server-only game rules, persistence, round flow, station setup, admin, NPC planning, and authoritative gameplay.
- `Content.Shared/`: shared components, predicted systems, networked events, prototype classes, and gameplay contracts.
- `Resources/`: data pack containing prototypes, maps, textures, audio, localization, config presets, keybinds, and server info.
- `RobustToolbox/`: engine source, including ECS, networking, map/entity serialization, UI framework, rendering, input, and content loading.

## Documentation Conventions

Most pages use the same sections:

- Purpose: what the subsystem does.
- Source Anchors: important files and directories.
- Runtime Flow: how data and control move through the system.
- Customization Levers: what to edit for reskins, features, removals, or replacement behavior.
- Fantasy Conversion Notes: implications for the Mythos conversion.
- Agent Search Terms: exact searches that quickly recover context.

## First-Pass Scope

This is not a full line-by-line manual for every system in the fork. The codebase has thousands of C# files, prototypes, textures, and maps. This pass creates a working atlas and deepens the areas most likely to matter for a total conversion:

- startup and module registration
- ECS and prototype data flow
- HUD, UI, themes, keybinds, and controls
- maps, map pools, station setup, and round flow
- player species, bodies, inventory, roles, loadouts, and access
- actions, verbs, interaction, combat, damage, health, and NPC AI
- assets, localization, and conversion levers


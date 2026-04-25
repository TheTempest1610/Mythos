# Mythos Audit Docs

This folder is an architecture atlas for converting Space Station 14 from hard sci-fi into the Mythos fantasy fork. It is written for AI agents first: every page favors source paths, symbols, prototype names, and search terms over polished public-facing prose.

The docs do not change gameplay behavior. They are a context database for future implementation work.

## How to Use This Atlas

- Start with [Architecture Overview](architecture/overview.md) to understand the project boundaries.
- Use [Mythos Extension Policy](architecture/mythos-extension-policy.md) before adding fork-specific code or touching upstream files.
- Use [Boot Lifecycle](architecture/boot-lifecycle.md) when changing startup, config, dependency registration, or round initialization.
- Use [ECS and Prototypes](architecture/ecs-prototypes.md) before editing components, systems, events, or YAML prototypes.
- Use [Networking and Prediction](architecture/networking-prediction.md) before adding replicated components, predictive events, or client/server UI messages.
- Use [Resources and Data](architecture/resources-data.md) before editing maps, textures, audio, localization, or config presets.
- Use [HUD, UI, and Themes](ui/hud-ui-theme.md) and [Input and Controls](ui/input-controls.md) for player-facing reskins and control changes.
- Use [Maps and Round Flow](gameplay/maps-round-flow.md), [Fantasy Mapping and Assets](gameplay/fantasy-mapping-assets.md), [Player Entities and Roles](gameplay/player-entities-roles.md), [Avatar Sprites and Customization](gameplay/avatar-sprites-customization.md), [Interactions, Combat, and NPCs](gameplay/interactions-combat-npc.md), and [Mythos Combat and Magic](gameplay/mythos-combat-magic.md) for gameplay conversion work.
- Use [Fantasy Change Levers](conversion/fantasy-change-levers.md) when deciding whether to reskin, reconfigure, extend, or remove an SS14 subsystem.
- Use [Testing and Verification](testing-verification.md) before changing behavior that already has Mythos tests.
- Use [Agent Search Index](agent-search-index.md) for exact `rg` searches.

## Repository Shape

The actual repository root is this directory's parent:

- `Content.Client/`: client-only game code, UI, rendering hooks, input registration, and client systems.
- `Content.Server/`: server-only game rules, persistence, round flow, station setup, admin, NPC planning, and authoritative gameplay.
- `Content.Shared/`: shared components, predicted systems, networked events, prototype classes, and gameplay contracts.
- `Resources/`: data pack containing prototypes, maps, textures, audio, localization, config presets, keybinds, and server info.
- `RobustToolbox/`: engine source, including ECS, networking, map/entity serialization, UI framework, rendering, input, and content loading.

## Mythos Fork Shape

Mythos-specific work is intentionally isolated:

- `Content.Shared/_Mythos/`: shared Mythos components, events, and systems. Namespace convention is mostly `Content.Shared.Mythos.*`.
- `Content.Server/_Mythos/`: server-authoritative Mythos systems, including combat queue, magic, player loadout, and tile placement support.
- `Content.Client/_Mythos/`: client presentation and predictive gameplay helpers, including combat targeting, queue execution, HUD overlays, and tile spawn UI.
- `Resources/Prototypes/_Mythos/`: Mythos YAML prototypes and upstream prototype overrides.
- `Resources/Textures/_Mythos/`: fantasy item/clothing/weapon RSI assets.
- `Resources/Textures/Tiles/_Mythos/`: flat tile PNGs generated from the OV import tooling.
- `Resources/Locale/en-US/_Mythos/`: Mythos localization keys.
- `Tools/_Mythos/`: conversion and validation scripts for imported fantasy assets.
- `Content.Tests/_Mythos/` and `Content.IntegrationTests/_Mythos/`: Mythos unit and integration tests.

Do not assume `_Mythos` directories are decorative. They are the preferred write surface for fork work.

## Documentation Conventions

Most pages use the same sections:

- Purpose: what the subsystem does.
- Source Anchors: important files and directories.
- Runtime Flow: how data and control move through the system.
- Customization Levers: what to edit for reskins, features, removals, or replacement behavior.
- Fantasy Conversion Notes: implications for the Mythos conversion.
- Agent Search Terms: exact searches that quickly recover context.

## External SS14 Docs Used

These docs are grounded in the official SS14 docs, but filtered for a fantasy total conversion. Prefer engine and workflow guidance over SS14-specific mechanics such as atmos pipe networks or cargo shuttle details.

- Codebase organization: https://docs.spacestation14.com/en/general-development/codebase-info/codebase-organization.html
- YAML crash course: https://docs.spacestation14.com/en/general-development/tips/yaml-crash-course.html
- ECS: https://docs.spacestation14.com/en/robust-toolbox/ecs.html
- Basic networking: https://docs.spacestation14.com/en/ss14-by-example/basic-networking-and-you.html
- Prediction: https://docs.spacestation14.com/en/ss14-by-example/prediction-guide.html
- Fluent/localization: https://docs.spacestation14.com/en/ss14-by-example/fluent-and-localization.html
- UI: https://docs.spacestation14.com/en/ss14-by-example/ui-and-you.html
- Sprites and icons: https://docs.spacestation14.com/en/robust-toolbox/rendering/sprites-and-icons.html
- RSI format: https://docs.spacestation14.com/en/specifications/robust-station-image.html
- Dynamic sprites: https://docs.spacestation14.com/en/ss14-by-example/making-a-sprite-dynamic.html
- Mapping guide: https://docs.spacestation14.com/en/space-station-14/mapping/guides/general-guide.html
- NPCs: https://docs.spacestation14.com/en/space-station-14/core-tech/npcs.html

## Scope

This is not a full line-by-line manual for every system in the fork. The codebase has thousands of C# files, prototypes, textures, and maps. The target is a working atlas that points agents at the right module, contract, search query, and local example before they change code.


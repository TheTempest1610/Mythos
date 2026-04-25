# Resources and Data

## Purpose

`Resources/` is the data pack. It contains most of the game surface that players see: entities, maps, textures, sounds, localization, keybinds, config presets, server info, and prototype definitions. A total conversion will spend most of its time here.

Official SS14 references:

- YAML: https://docs.spacestation14.com/en/general-development/tips/yaml-crash-course.html
- Localization: https://docs.spacestation14.com/en/ss14-by-example/fluent-and-localization.html
- Sprites/icons: https://docs.spacestation14.com/en/robust-toolbox/rendering/sprites-and-icons.html
- RSI format: https://docs.spacestation14.com/en/specifications/robust-station-image.html

## Source Anchors

- `Resources/manifest.yml`
- `Resources/keybinds.yml`
- `Resources/migration.yml`
- `Resources/Prototypes/`
- `Resources/Maps/`
- `Resources/Textures/`
- `Resources/Audio/`
- `Resources/Locale/`
- `Resources/ConfigPresets/`
- `Resources/IgnoredPrototypes/`
- `Resources/Prototypes/_Mythos/`
- `Resources/Textures/_Mythos/`
- `Resources/Textures/Tiles/_Mythos/`
- `Resources/Locale/en-US/_Mythos/`
- `Tools/_Mythos/ov_port/`
- `RobustToolbox/Schemas/`
- `Content.Shared/Localizations/ContentLocalizationManager.cs`
- `Content.Shared/Entry/EntryPoint.cs`

## Runtime Flow

The engine resource manager exposes content files to client, server, and shared initialization. Prototypes are loaded from `Resources/Prototypes/` and mapped to C# prototype classes or entity prototypes. Some prototype paths are ignored through `Resources/IgnoredPrototypes/` and entrypoint-level `RegisterIgnore` calls because they are side-specific or intentionally hidden.

Maps live in `Resources/Maps/` as serialized YAML map files. Map prototypes in `Resources/Prototypes/Maps/` point at those files and add gameplay metadata such as map name, station configs, player limits, and eligibility rules.

Textures usually live in `.rsi` folders under `Resources/Textures/`. Each `.rsi` folder contains `meta.json` describing states, frame counts, directions, and attribution. UI themes point at interface texture directories such as `/Textures/Interface/Default/`.

For humanoid sprites, do not treat RSI files as standalone attachments. The body, markings, and equipment are all merged into one layered wearer `SpriteComponent`. See [Avatar Sprites and Customization](../gameplay/avatar-sprites-customization.md) before editing player body parts, hair, tails, ears, helmets, hoods, or on-mob clothing.

Localization uses Fluent `.ftl` files under `Resources/Locale/<culture>/`. Prototype display names, UI labels, action names, role descriptions, guidebook text, and popups often refer to localization keys instead of literal text.

Config presets under `Resources/ConfigPresets/` are loaded by server startup and can override defaults for development, debug, or hosted server variants.

Mythos adds a data-isolation layer:

- `Resources/Prototypes/_Mythos/Actions/combat_spells.yml`: spell action entry points.
- `Resources/Prototypes/_Mythos/Tiles/_OV/`: imported fantasy tile prototypes.
- `Resources/Textures/Tiles/_Mythos/_OV/`: generated flat tile PNGs.
- `Resources/Textures/_Mythos/_OV/`: imported fantasy RSI assets, especially Roguetown clothing/weapons/turf.
- `Resources/Locale/en-US/_Mythos/tiles/ov-tiles.ftl`: tile display names.

## Customization Levers

- Prototypes: change gameplay identity, entity composition, jobs, species, maps, recipes, alerts, damage, radio channels, access, tiles, and themes.
- Maps: replace station layouts with fantasy settlements, dungeons, ships, keeps, wildlands, or hubs.
- Textures: replace RSI sprites and interface textures while keeping state names stable for low-risk reskins.
- Audio: replace sound collections and sound paths with fantasy ambience, combat, UI, and item sounds.
- Locale: rename departments, jobs, items, verbs, alerts, damage text, popups, and guidebook entries.
- Config presets: set local development defaults for maps, presets, lobby, auth, and server behavior.
- Import tools: regenerate OV tile prototypes and locale seeds through `Tools/_Mythos/ov_port/`.

## Data Examples

Localized string key in C#:

```csharp
Loc.GetString("mythos-spell-no-mana", ("spell", spellName));
```

Fluent entry:

```fluent
mythos-spell-no-mana = You do not have enough mana to cast {$spell}.
```

RSI-backed entity sprite:

```yaml
- type: Sprite
  sprite: _Mythos/_OV/Roguetown/Weapons/swords32.rsi
  state: longsword
```

Explicit on-mob clothing visual:

```yaml
- type: Clothing
  slots: [ OUTERCLOTHING ]
  clothingVisuals:
    outerClothing:
    - sprite: _Mythos/_OV/Roguetown/Clothing/onmob/armor.rsi
      state: cuirass
```

Flat tile sprite:

```yaml
- type: tile
  id: FloorMythosRoguefloorCobblestone
  name: tiles-ov-roguefloor-cobblestone
  sprite: /Textures/Tiles/_Mythos/_OV/roguefloor/cobblestone.png
```

Action event tag:

```yaml
- type: InstantAction
  event: !type:QueueFireballActionEvent
```

## Fantasy Conversion Notes

Treat `Resources/` as the first demolition layer. Many sci-fi terms can be removed without touching C# if they only appear in prototype IDs, localized names, descriptions, sprites, sounds, and map content. C# changes become necessary when behavior assumes a domain, such as station power grids, atmos gas simulation, radio channels, ID access, departments, cargo economy, cloning, or medical devices.

Avoid deleting broad prototype families until dependent maps and systems are audited. Missing prototypes break map loading, entity spawning, guidebook generation, and test data. Use replacement prototypes or ignore lists during staged removal.

Keep generated and manual assets separate. Imported OV tiles are generated and can be re-run. Spell action prototypes are manual gameplay data and should be reviewed like code.

## Agent Search Terms

```powershell
rg --files Resources\\Prototypes Resources\\Maps Resources\\Textures Resources\\Locale
rg -n "type: .*|id: .*|parent: .*|components:" Resources\\Prototypes -g "*.yml"
rg -n "mapPath:|stationProto:|availableJobs:|gameMapPool" Resources\\Prototypes\\Maps -g "*.yml"
rg -n "sprite:|state:|sound:|path:" Resources\\Prototypes -g "*.yml"
rg -n "clothingVisuals:|type: marking|bodyPart:|BaseSpeciesLayers|VisualOrganMarkings" Resources\\Prototypes -g "*.yml"
rg --files Resources\\Textures\\Mobs\\Species Resources\\Textures\\Mobs\\Customization Resources\\Textures\\_Mythos | rg "\\.rsi(\\\\|/)(meta\\.json|.*\\.png)$"
rg -n "^[a-zA-Z0-9_.-]+\\s*=" Resources\\Locale\\en-US -g "*.ftl"
rg --files Resources\\Prototypes\\_Mythos Resources\\Textures\\_Mythos Resources\\Textures\\Tiles\\_Mythos Resources\\Locale\\en-US\\_Mythos Tools\\_Mythos
rg -n "ActionMythos|Queue.*ActionEvent|tiles-ov-|sprite: /Textures/Tiles/_Mythos" Resources\\Prototypes\\_Mythos Resources\\Locale\\en-US\\_Mythos -g "*.yml" -g "*.ftl"
```


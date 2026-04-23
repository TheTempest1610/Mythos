# Player Entities and Roles

## Purpose

This page maps the player entity chain: species, body parts, humanoid appearance, inventory, hands, movement, jobs, loadouts, access, and spawning. Use it when replacing humans, adding fantasy ancestries/classes, changing equipment slots, or converting departments into fantasy organizations.

## Source Anchors

- `Resources/Prototypes/Species/`
- `Resources/Prototypes/Body/species_base.yml`
- `Resources/Prototypes/Body/species_appearance.yml`
- `Resources/Prototypes/Body/base_organs.yml`
- `Resources/Prototypes/Body/Species/human.yml`
- `Resources/Prototypes/Entities/Mobs/base.yml`
- `Resources/Prototypes/Entities/Mobs/Player/`
- `Resources/Prototypes/Roles/Jobs/`
- `Resources/Prototypes/Roles/Jobs/departments.yml`
- `Resources/Prototypes/Loadouts/`
- `Resources/Prototypes/Access/`
- `Content.Shared/Humanoid/`
- `Content.Shared/Body/`
- `Content.Shared/Inventory/`
- `Content.Shared/Hands/`
- `Content.Shared/Roles/`
- `Content.Server/Station/Systems/StationJobsSystem.cs`
- `Content.Server/Station/Systems/StationSpawningSystem.cs`
- `Content.Shared/Station/SharedStationSpawningSystem.cs`

## Runtime Flow

Player bodies are prototype-composed. `Resources/Prototypes/Species/human.yml` defines the `Human` species and points at `MobHuman` and `AppearanceHuman`. `Resources/Prototypes/Body/Species/human.yml` defines human appearance, initial organs, body parts, and the `MobHuman` entity.

The base mob chain starts in `Resources/Prototypes/Entities/Mobs/base.yml`. It defines controllable movement, clickability, interactions, eye/camera behavior, damage capability, combat, atmosphere exposure, bloodstream, respiration, and other common mob behavior. `Resources/Prototypes/Body/species_base.yml` adds humanoid-specific layers: identity, typing indicator, damage visuals, inventory UI, speech, faction membership, stripping, pulling, and tags.

Jobs are data prototypes under `Resources/Prototypes/Roles/Jobs/`. A job defines localized name/description, playtime tracker, starting gear, icon, supervisors, access, and other requirements. Starting gear prototypes equip initial items into inventory slots.

Round spawning combines selected jobs, station job availability, profile/species data, spawn points, and loadouts. Station systems own job slots and station membership. Shared station spawning provides common contracts and events.

## Customization Levers

- New ancestry/species: add a species prototype, body/organ prototypes, appearance prototype, sprites, markings, localization, and preferences support if needed.
- New class/job: add job prototype, starting gear, loadout groups, job icon, access/permissions, localized names, and station availability.
- New equipment model: edit inventory templates and inventory components carefully; many UI and equip systems depend on slot names.
- New spawn model: adjust station spawning, spawn point prototypes, and map markers.
- New faction or social structure: edit jobs, departments, access groups, station configs, and NPC factions.
- Player-facing names: update locale keys, not just prototype IDs.

## Fantasy Conversion Notes

Map old concepts to new ones before deleting:

- species can become ancestry, lineage, kin, or mortal form
- jobs can become classes, callings, guild roles, court offices, or professions
- departments can become guilds, houses, temples, orders, schools, or settlements
- access can become keys, permissions, oaths, sigils, rank, or wards
- loadouts can become class kits, heirlooms, starting spells, guild equipment, or background gear

The strongest conversion path is to keep humanoid/inventory/hands systems while replacing the content layer. A human can become a fantasy commoner through names, sprites, body details, jobs, loadouts, and faction data before any deep code rewrite.

## Agent Search Terms

```powershell
rg -n "type: species|prototype:|dollPrototype:|skinColoration:" Resources\\Prototypes\\Species -g "*.yml"
rg -n "id: MobHuman|BaseSpeciesMob|BaseControllable|InitialBody|HumanoidProfile|Inventory" Resources\\Prototypes\\Body Resources\\Prototypes\\Entities\\Mobs -g "*.yml"
rg -n "type: job|startingGear|access:|playTimeTracker|supervisors|departments" Resources\\Prototypes\\Roles\\Jobs -g "*.yml"
rg -n "type: startingGear|type: loadout|type: loadoutGroup|equipment:" Resources\\Prototypes\\Loadouts Resources\\Prototypes\\Roles -g "*.yml"
rg -n "StationJobs|StationSpawning|PlayerBeforeSpawn|PlayerSpawnComplete|SpawnPoint" Content.Server Content.Shared
```


# Fantasy Change Levers

## Purpose

This page translates SS14 subsystem areas into Mythos conversion decisions. Use it to decide whether a system should be reskinned, reconfigured, extended, wrapped, or removed.

## Source Anchors

- `Docs/architecture/overview.md`
- `Docs/architecture/ecs-prototypes.md`
- `Docs/architecture/resources-data.md`
- `Docs/ui/hud-ui-theme.md`
- `Docs/gameplay/maps-round-flow.md`
- `Docs/gameplay/player-entities-roles.md`
- `Docs/gameplay/interactions-combat-npc.md`
- `Resources/Prototypes/`
- `Resources/Locale/en-US/`
- `Resources/Textures/`
- `Content.Client/`
- `Content.Server/`
- `Content.Shared/`

## Runtime Flow

Most conversion work follows this stack:

1. Rename or reskin the player-facing layer: locale, sprites, icons, UI themes, map names, role names.
2. Reconfigure data: prototypes for entities, jobs, loadouts, maps, access, recipes, damage, alerts, and NPCs.
3. Extend systems: new components, new actions, new verbs, new prototype classes, new server rules.
4. Replace domain systems: remove sci-fi mechanics only when maps, prototypes, UI, and dependent systems have replacements.
5. Touch engine code only for capabilities the content layer cannot express.

Mythos-specific extension order:

1. Add new fork code under `Content.Client/_Mythos`, `Content.Server/_Mythos`, or `Content.Shared/_Mythos`.
2. Add new or override data under `Resources/Prototypes/_Mythos`.
3. Add assets under `Resources/Textures/_Mythos` or `Resources/Textures/Tiles/_Mythos`.
4. Add localized strings under `Resources/Locale/en-US/_Mythos` when practical.
5. Add a tiny tagged upstream hook only if the existing system needs to call into the fork.
6. Add tests under `Content.Tests/_Mythos` or `Content.IntegrationTests/_Mythos`.

## Customization Levers

Use this decision table:

| SS14 area | Fantasy target | First lever | Rewrite trigger |
| --- | --- | --- | --- |
| Station maps | settlements, dungeons, keeps, ships, realms | map files and map prototypes | station entity assumptions block new round flow |
| Jobs/departments | classes, guilds, houses, orders | job, department, loadout, locale prototypes | role rules no longer fit job model |
| ID/access | keys, sigils, permissions, wards | access prototypes, ID items, door prototypes | access needs spatial magic or social simulation |
| PDA/computers | spellbooks, ledgers, guild tools | UI labels, sprites, bound UI windows | workflow is unrelated to existing apps |
| Radio/comms | speech channels, messengers, telepathy | radio channel prototypes and chat systems | communication model changes deeply |
| Power | mana, hearthfire, infrastructure, remove | prototypes and machine dependencies | grid simulation no longer applies |
| Atmos | weather, breath, poison, hazards, remove | map/prototype hazards and atmos configs | tile gas sim is not desired |
| Cargo/economy | markets, caravans, guild requisitions | cargo product/account/bounty prototypes | order flow and currency model change |
| Medical/cloning | healing, resurrection, curses | damage/status/medical prototypes and locale | body/death loop changes |
| Security | guards, laws, reputation, factions | jobs, access, contraband, records | crime system becomes social simulation |
| Science/research | arcana, crafting, discoveries | research prototypes and UI labels | tech tree model changes |
| NPC mobs | monsters, summons, companions | NPC prototypes, factions, HTN tasks | AI goals need new planners |
| Weapons | melee, bows, spells, relics | weapon/action/damage prototypes | timing, targeting, or resource mechanics change |
| UI | parchment, runes, guild menus | themes, textures, stylesheets | layout/workflow no longer fits |
| Atmos simulation | weather, poison, breath rules, disabled by default | `mythos.atmos.enabled` CVar and map/prototype cleanup | fantasy survival model replaces SS14 gas sim |
| Spell actions | class abilities, prayers, rituals | action prototypes plus combat queue | per-spell tuning/resource model outgrows hardcoded constants |
| Mapping tiles | fantasy terrain | OV tile import tools and `_Mythos` tile prototypes | tile metadata needs hand-authored gameplay semantics |

## Fantasy Conversion Notes

Prefer reversible layers early:

- Keep prototype IDs stable where maps depend on them, but change localized names and sprites.
- Add fantasy variants before deleting sci-fi originals.
- Use parent prototypes to redirect broad families of content.
- Keep station, job, and access internals temporarily if they can support "settlement", "role", and "permission" semantics.
- Preserve existing tests and map loadability while removing content in stages.

Likely early Mythos docs/tasks after this atlas:

- Create a fantasy terminology glossary mapping old SS14 concepts to Mythos terms.
- Create a prototype dependency map for jobs, departments, access, and map station configs.
- Create a UI reskin checklist for theme prototypes, interface textures, and stylesheet palettes.
- Create a first playable fantasy map pool with one development map and one small role set.

Current Mythos gameplay levers already implemented:

- `ManaComponent`: shared lazy-regenerating resource pool.
- `CombatTargetComponent`: selected target replicated to client.
- `CombatQueueComponent`: predicted FIFO action queue with monotonic slot IDs.
- `ActionMythosMagicMissile` and `ActionMythosFireball`: stock action hotbar entries feeding the queue.
- `mythos.atmos.enabled`: staged disable switch for SS14 atmos-related runtime systems.
- Variant-aware tile spawn UI: mapper productivity tool for imported fantasy tiles.

Use these as patterns for new systems.

## Agent Search Terms

```powershell
rg -n "NanoTrasen|Syndicate|station|captain|security|engineering|medical|science|cargo" Resources Content.Client Content.Server Content.Shared
rg -n "PDA|IDCard|Access|Radio|Power|Atmos|Cargo|Research|Cloning|Security" Content.Client Content.Server Content.Shared Resources\\Prototypes
rg -n "job-name-|department-|access-|reagent-name-|ent-|ui-|alert-" Resources\\Locale\\en-US -g "*.ftl"
rg -n "sprite:|icon:|sound:|mapName:|name:|description:" Resources\\Prototypes -g "*.yml"
rg -n "ManaComponent|CombatTargetComponent|CombatQueueComponent|ActionMythos|mythos\\.atmos|MsgSetTileVariantOverride" Content.Client Content.Server Content.Shared Resources\\Prototypes
```


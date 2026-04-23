# Interactions, Combat, and NPCs

## Purpose

This page groups the most important moment-to-moment gameplay systems: clicking, verbs, hands, inventory/storage, actions, combat, damage, health state, and NPC AI. Use it when adding fantasy abilities, melee/ranged systems, monsters, companions, rituals, harvesting, or new interaction verbs.

## Source Anchors

- `Content.Client/Gameplay/GameplayStateBase.cs`
- `Content.Shared/Interaction/SharedInteractionSystem.cs`
- `Content.Client/ContextMenu/UI/EntityMenuUIController.cs`
- `Content.Client/Verbs/UI/VerbMenuUIController.cs`
- `Content.Shared/Verbs/`
- `Content.Shared/Hands/`
- `Content.Server/Hands/`
- `Content.Shared/Inventory/`
- `Content.Client/UserInterface/Systems/Inventory/`
- `Content.Shared/Storage/`
- `Content.Client/UserInterface/Systems/Storage/`
- `Content.Shared/Actions/`
- `Content.Client/UserInterface/Systems/Actions/`
- `Content.Shared/Weapons/`
- `Content.Shared/Damage/`
- `Content.Shared/Mobs/`
- `Content.Server/NPC/`
- `Content.Shared/NPC/`
- `Resources/Prototypes/NPCs/`
- `Resources/Prototypes/Entities/Mobs/NPCs/`

## Runtime Flow

Viewport clicks start in the client. `GameplayStateBase` finds clickable entities under the cursor, orders them by draw depth/rendering, and routes input into command handlers. Interaction systems then decide whether the player can use, alt-use, pull, examine, or open context menus.

Verbs are contextual actions exposed by systems. They power right-click menus and entity context actions. Use verbs for "do something to this target" interactions that are not always visible as hotbar abilities.

Hands and inventory are central to SS14 interaction. Hands hold active items. Inventory slots equip clothing, tools, IDs, bags, and containers. Storage systems handle grids, item movement, nested containers, and UI windows.

The actions system handles explicit abilities, including hotbar buttons, cooldowns, toggle states, target selection, and action UI presentation. It is the best existing path for spells and class abilities.

Combat and health are component-driven. Weapons and melee systems apply damage. `DamageableSystem` modifies damage state. Mob systems evaluate thresholds such as alive, critical, and dead. Stamina, status effects, passive damage, temperature, atmosphere, bloodstream, and medical systems all layer onto this.

NPCs use server-side AI systems. NPC prototypes define HTN tasks, utility queries, factions, combat behavior, perception, steering, pathfinding, and mob entities. Shared NPC code exposes faction and path/debug contracts.

## Customization Levers

- Add a contextual interaction: add or extend a verb/event in the appropriate system.
- Add a spell or class skill: use the actions system, action prototypes/components, and `ActionUIController` hotbar integration.
- Add a new weapon family: compose existing melee/ranged/damage components first, then add systems only for unique behavior.
- Add monster AI: start with NPC entity prototypes, factions, HTN tasks, utility queries, and combat components.
- Add containers or equipment: use existing inventory/storage components and UI before changing slot architecture.
- Change health model: edit damage prototypes and mob thresholds carefully; many systems observe critical/dead states.

## Fantasy Conversion Notes

Fantasy gameplay can reuse most interaction infrastructure:

- spells: actions with target, instant, or world behaviors
- rituals: verbs, do-afters, entity interactions, and stateful components
- equipment: inventory slots, hands, storage, wielding, and clothing components
- monsters: NPC prototypes plus factions, HTN tasks, and melee/ranged combat
- status effects: existing damage, stamina, alerts, and status effect systems
- harvesting/crafting: verbs, construction, recipes, storage, and material systems

Be cautious when replacing combat. Damage type names and groups are data-driven, but many medical, UI, and mob systems assume damage containers, thresholds, and mob states. Rename/reskin damage in data first; change system semantics only after the target health model is designed.

## Agent Search Terms

```powershell
rg -n "GetClickedEntity|Clickable|UseSecondary|ActivateItemInWorld|AltActivateItemInWorld" Content.Client Content.Shared
rg -n "Verb|GetVerbs|EntityMenu|VerbMenu" Content.Client Content.Shared Content.Server
rg -n "Action|InstantAction|TargetAction|WorldTargetAction|OpenActionsMenu|Hotbar" Content.Client Content.Shared Resources\\Prototypes
rg -n "Damageable|DamageSpecifier|MobThresholds|MobState|Stamina|StatusEffects|PassiveDamage" Content.Shared Content.Server Resources\\Prototypes
rg -n "HTN|NPC|NpcFaction|UtilityQuery|NPCMeleeCombat|NPCRangedCombat|NPCSteering" Content.Server Content.Shared Resources\\Prototypes
```


# ECS and Prototypes

## Purpose

SS14 gameplay is built around RobustToolbox ECS plus YAML prototypes. Entities are composed from components. Systems subscribe to events, update component state, and apply game rules. Prototypes define reusable entity and data templates that the engine deserializes into components and prototype classes.

Official SS14 references:

- https://docs.spacestation14.com/en/robust-toolbox/ecs.html
- https://docs.spacestation14.com/en/general-development/tips/yaml-crash-course.html
- https://docs.spacestation14.com/en/robust-toolbox/serialization.html

## Source Anchors

- `RobustToolbox/Robust.Shared/GameObjects/Component.cs`
- `RobustToolbox/Robust.Shared/GameObjects/EntitySystem.cs`
- `RobustToolbox/Robust.Shared/GameObjects/EntitySystem.Subscriptions.cs`
- `RobustToolbox/Robust.Shared/GameObjects/EntityEventBus.*.cs`
- `RobustToolbox/Robust.Shared/GameObjects/ComponentFactory.cs`
- `RobustToolbox/Robust.Shared/Prototypes/`
- `Content.Shared/*/Components/*.cs`
- `Content.Shared/*/Systems/*.cs`
- `Content.Server/*/Systems/*.cs`
- `Content.Client/*/*System.cs`
- `Resources/Prototypes/Entities/`
- `Resources/Prototypes/*.yml`
- `Content.Shared/_Mythos/Combat/Queue/CombatQueueComponent.cs`
- `Content.Shared/_Mythos/Magic/Mana/ManaComponent.cs`
- `Resources/Prototypes/_Mythos/Actions/combat_spells.yml`

## Runtime Flow

Components are mostly data. A component class inherits `Component`, is registered through component registration, and is deserialized from prototype YAML or added at runtime. Components can be networked and marked dirty so clients receive updated state.

Systems own behavior. An `EntitySystem` is dependency-injected, initialized by the entity system manager, and subscribes to events. Common event patterns include:

- component lifecycle events such as `ComponentInit`, `ComponentStartup`, and `ComponentShutdown`
- directed entity events raised on entities with a component
- broadcast local events
- network events between client and server
- input command events and UI messages
- map init and round flow events

Prototypes provide data. Entity prototypes use `type: entity`, optional `parent`, `id`, `abstract`, `components`, and other metadata. Non-entity prototypes are backed by C# prototype classes marked with `[Prototype]`, such as species, jobs, maps, damage types, reagents, alerts, themes, and construction graphs.

Shared code is the contract surface. Server systems usually make final decisions. Client systems generally present state, predict interactions, or send requests. Shared systems are used when both sides need the same logic or event contracts.

SS14's official ECS docs emphasize composition over deep inheritance. For Mythos, that means new fantasy features should usually be components plus systems, not new base entity classes. A "caster" is a mob with `ManaComponent`, spell actions, and queue state. A "settlement" can remain an internal station entity with different prototypes and locale while the round-flow replacement is staged.

## Customization Levers

- Add simple new content by composing existing components in YAML.
- Add new behavior by creating a component plus a system that subscribes to events.
- Add a new data type by creating an `[Prototype]` class in shared or server/client scope, then defining YAML under `Resources/Prototypes/`.
- Modify balance and presentation by changing `DataField` values in YAML before changing C#.
- Use abstract parent prototypes for broad conversion layers, such as fantasy base doors, fantasy humanoids, faction mobs, or magic devices.
- Use events to connect systems rather than direct hard dependencies when behavior should remain modular.
- Put Mythos component state mutations behind a shared system method when client prediction or tests need the same behavior.
- Put same-ID upstream prototype overrides under `Resources/Prototypes/_Mythos/`.

## Local Code Examples

Networked component:

```csharp
[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class ManaComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Max = 100f;

    [DataField, AutoNetworkedField]
    public float Current = 100f;
}
```

Shared mutation system:

```csharp
public bool TrySpend(EntityUid uid, float amount, ManaComponent? comp = null)
{
    if (!Resolve(uid, ref comp, false))
        return false;

    var effective = CalculateEffectiveMana(comp, Timing.CurTime);
    if (effective < amount)
        return false;

    comp.Current = effective - amount;
    Dirty(uid, comp);
    return true;
}
```

Action prototype with a typed event:

```yaml
- type: entity
  parent: BaseAction
  id: ActionMythosMagicMissile
  components:
  - type: Action
    icon:
      sprite: Objects/Magic/magicactions.rsi
      state: magicmissile
  - type: InstantAction
    event: !type:QueueMagicMissileActionEvent
```

## Fantasy Conversion Notes

For Mythos, keep the ECS pattern intact. The conversion should produce fantasy versions of the existing prototype hierarchies instead of rewriting entity creation from scratch.

Likely high-value patterns:

- Replace sci-fi item families by making new fantasy parent prototypes that reuse inventory, interaction, storage, damage, and sprite components.
- Replace machine behavior by keeping `UserInterface`, power/storage/interaction patterns only where useful and swapping domain systems behind them.
- Replace role/job data first, then adjust systems that assume departments, access, comms, station IDs, or technology.
- Create fantasy-specific components for magic, class abilities, factions, resources, quests, or rituals, then expose them through actions and UI.

Be careful with networked components. Any client-visible component or event must exist in shared code and be understood by both sides. Server-only prototypes can be ignored by the client entrypoint, but shared gameplay data cannot.

YAML caution: SS14 YAML uses custom tags such as `!type:QueueMagicMissileActionEvent`. Generic YAML formatters often do not understand these tags. Do not auto-format prototype files unless the tool preserves custom tags, indentation, and lists.

## Agent Search Terms

```powershell
rg -n "class .*Component|sealed partial class .*Component|\\[RegisterComponent\\]" Content.Client Content.Server Content.Shared
rg -n "class .*System|sealed class .*System|EntitySystem" Content.Client Content.Server Content.Shared
rg -n "SubscribeLocalEvent|SubscribeNetworkEvent|RaiseLocalEvent|RaiseNetworkEvent|Dirty\\(" Content.Client Content.Server Content.Shared
rg -n "\\[Prototype|IPrototype|IdDataField|DataField" Content.Client Content.Server Content.Shared
rg -n "type: entity|parent:|components:" Resources\\Prototypes\\Entities -g "*.yml"
rg -n "ManaComponent|CombatQueueComponent|CombatTargetComponent|QueuedAction|ActionMythos" Content.Shared\\_Mythos Resources\\Prototypes\\_Mythos -g "*.cs" -g "*.yml"
```


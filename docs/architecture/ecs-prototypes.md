# ECS and Prototypes

## Purpose

SS14 gameplay is built around RobustToolbox ECS plus YAML prototypes. Entities are composed from components. Systems subscribe to events, update component state, and apply game rules. Prototypes define reusable entity and data templates that the engine deserializes into components and prototype classes.

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

## Customization Levers

- Add simple new content by composing existing components in YAML.
- Add new behavior by creating a component plus a system that subscribes to events.
- Add a new data type by creating an `[Prototype]` class in shared or server/client scope, then defining YAML under `Resources/Prototypes/`.
- Modify balance and presentation by changing `DataField` values in YAML before changing C#.
- Use abstract parent prototypes for broad conversion layers, such as fantasy base doors, fantasy humanoids, faction mobs, or magic devices.
- Use events to connect systems rather than direct hard dependencies when behavior should remain modular.

## Fantasy Conversion Notes

For Mythos, keep the ECS pattern intact. The conversion should produce fantasy versions of the existing prototype hierarchies instead of rewriting entity creation from scratch.

Likely high-value patterns:

- Replace sci-fi item families by making new fantasy parent prototypes that reuse inventory, interaction, storage, damage, and sprite components.
- Replace machine behavior by keeping `UserInterface`, power/storage/interaction patterns only where useful and swapping domain systems behind them.
- Replace role/job data first, then adjust systems that assume departments, access, comms, station IDs, or technology.
- Create fantasy-specific components for magic, class abilities, factions, resources, quests, or rituals, then expose them through actions and UI.

Be careful with networked components. Any client-visible component or event must exist in shared code and be understood by both sides. Server-only prototypes can be ignored by the client entrypoint, but shared gameplay data cannot.

## Agent Search Terms

```powershell
rg -n "class .*Component|sealed partial class .*Component|\\[RegisterComponent\\]" Content.Client Content.Server Content.Shared
rg -n "class .*System|sealed class .*System|EntitySystem" Content.Client Content.Server Content.Shared
rg -n "SubscribeLocalEvent|SubscribeNetworkEvent|RaiseLocalEvent|RaiseNetworkEvent|Dirty\\(" Content.Client Content.Server Content.Shared
rg -n "\\[Prototype|IPrototype|IdDataField|DataField" Content.Client Content.Server Content.Shared
rg -n "type: entity|parent:|components:" Resources\\Prototypes\\Entities -g "*.yml"
```


# Mythos Extension Policy

## Purpose

This page defines how agents should add Mythos features while keeping upstream SS14 merges survivable. The rule is simple: put new fork code in `_Mythos` first, use data overrides where possible, and make upstream edits small, tagged, and discoverable.

## Source Anchors

- `Content.Shared/_Mythos/README.md`
- `Content.Server/_Mythos/README.md`
- `Content.Client/_Mythos/README.md`
- `Resources/Prototypes/_Mythos/README.md`
- `Content.Tests/_Mythos/README.md`
- `Content.IntegrationTests/_Mythos/README.md`
- `Content.Shared/CCVar/CCVars.Mythos.cs`
- `Content.Client/UserInterface/Systems/Sandbox/SandboxUIController.cs`
- `Content.Shared/StationRecords/StationRecordSet.cs`

## Runtime Shape

RobustToolbox scans the content assemblies, not directory names. A component or system in `Content.Shared/_Mythos/Combat/Queue/` registers normally as long as it has the correct attributes and a loadable concrete type.

Use these folder-to-namespace conventions:

| Folder | Normal namespace | Purpose |
| --- | --- | --- |
| `Content.Shared/_Mythos/` | `Content.Shared.Mythos.*` | Networked components, shared systems, events, data contracts |
| `Content.Server/_Mythos/` | `Content.Server.Mythos.*` | Authoritative systems, round/player grants, damage, persistence |
| `Content.Client/_Mythos/` | `Content.Client.Mythos.*` | Presentation, overlays, UI controllers, predictive local helpers |
| `Resources/Prototypes/_Mythos/` | n/a | New prototypes and same-ID upstream overrides |

Existing tile spawn code uses `Content.*._Mythos.TileSpawn` namespaces. Treat that as local precedent for that subsystem only.

## Upstream Touch Policy

Preferred order:

1. Add a cancellable event hook to upstream and subscribe from a Mythos system.
2. Make an upstream type `partial` and place fork logic in a Mythos sibling file.
3. Inline edit upstream only when the first two do not fit.

Every upstream edit must include `// Mythos: <reason>` on or near the changed line. This makes merge audits one command:

```powershell
rg -n "// Mythos:" Content.Client Content.Server Content.Shared
```

Current upstream touches include:

- `Content.Client/UserInterface/Systems/Sandbox/SandboxUIController.cs`: uses the Mythos tile spawn controller.
- `Content.Shared/CCVar/CCVars.Mythos.cs`: adds `mythos.atmos.enabled`.
- `Content.Server/Atmos/EntitySystems/AtmosphereSystem.cs`: skips atmos update when the Mythos gate is false.
- `Content.Server/Atmos/EntitySystems/BarotraumaSystem.cs`: skips barotrauma update when the Mythos gate is false.
- `Content.Server/Atmos/EntitySystems/FlammableSystem.cs`: skips flammable update when the Mythos gate is false.
- `Content.Server/Body/Systems/RespiratorSystem.cs`: skips respiration when the Mythos atmos gate is false.
- `Content.Server/Body/Systems/ThermalRegulatorSystem.cs`: skips thermal regulator update when the Mythos gate is false.
- `Content.Server/Temperature/Systems/TemperatureSystem.cs`: skips temperature update when the Mythos gate is false.
- `Content.Shared/StationRecords/StationRecordSet.cs`: removes serialization from a `Dictionary<Type,...>` field that cannot serialize cleanly.

## Prototype Override Policy

SS14 resolves prototypes by ID, not path. To override upstream data, create a file under `Resources/Prototypes/_Mythos/` with the same `id`. To add new content, use Mythos-specific IDs such as `ActionMythosFireball` to avoid accidental collisions.

Example new action prototype:

```yaml
- type: entity
  parent: BaseAction
  id: ActionMythosExampleSpell
  name: Example Spell
  components:
  - type: Action
    icon:
      sprite: Objects/Magic/magicactions.rsi
      state: magicmissile
  - type: InstantAction
    event: !type:QueueMagicMissileActionEvent
```

Example same-ID override:

```yaml
- type: entity
  id: ClothingHeadHelmetBasic
  name: iron kettle helm
  components:
  - type: Sprite
    sprite: _Mythos/Roguetown/Clothing/head.rsi
```

Use same-ID overrides cautiously. If maps depend on upstream behavior, verify map loading and any tests that spawn the entity.

## Feature Gates

Use CVars for broad subsystem gates. Current example:

```csharp
public static readonly CVarDef<bool> MythosAtmosEnabled =
    CVarDef.Create("mythos.atmos.enabled", true, CVar.SERVERONLY);
```

The atmos gate keeps upstream prototypes intact while server systems no-op. This is the right shape for staged removal of SS14-specific mechanics: disable runtime behavior first, then replace maps, prototypes, UI, and tests in smaller passes.

## Agent Rules

- Add new fantasy gameplay under `_Mythos` unless an upstream system truly needs a hook.
- Prefer shared components/events when the client must see, predict, or display the state.
- Keep server authority for damage, resource grants, player attachment, persistence, and round flow.
- Do not delete upstream content families until maps and prototypes no longer reference them.
- Do not add new `misc` folders. Mirror the existing domain shape: `Magic/Mana`, `Combat/Queue`, `TileSpawn`, etc.
- Add or update Mythos tests for any new gameplay contract.

## Agent Search Terms

```powershell
rg --files Content.Client\_Mythos Content.Server\_Mythos Content.Shared\_Mythos Resources\Prototypes\_Mythos
rg -n "// Mythos:" Content.Client Content.Server Content.Shared
rg -n "CVarDef.Create\\(\"mythos\\.|MythosAtmosEnabled|RegisterComponent|NetworkedComponent" Content.Client Content.Server Content.Shared
rg -n "id: ActionMythos|id: Mythos|_Mythos" Resources\Prototypes\_Mythos -g "*.yml"
```

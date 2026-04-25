# Mythos Combat and Magic

## Purpose

This page explains the current Mythos gameplay layer: target selection, auto-attack, combat queue, mana, spell actions, Magic Missile, Fireball, and the HUD overlays. Use it before adding spells, class abilities, resource systems, or combat UI.

## Source Anchors

- `Content.Shared/_Mythos/Combat/Targeting/CombatTargetComponent.cs`
- `Content.Shared/_Mythos/Combat/Targeting/SharedCombatTargetSystem.cs`
- `Content.Client/_Mythos/Combat/Targeting/CombatTargetClickSystem.cs`
- `Content.Client/_Mythos/Combat/Targeting/CombatAutoAttackSystem.cs`
- `Content.Server/_Mythos/Combat/Targeting/CombatTargetSystem.cs`
- `Content.Client/_Mythos/Combat/Targeting/TargetReticleOverlay.cs`
- `Content.Shared/_Mythos/Combat/Queue/CombatQueueComponent.cs`
- `Content.Shared/_Mythos/Combat/Queue/QueuedAction.cs`
- `Content.Shared/_Mythos/Combat/Queue/SharedCombatQueueSystem.cs`
- `Content.Client/_Mythos/Combat/Queue/CombatQueueExecutor.cs`
- `Content.Shared/_Mythos/Magic/Mana/ManaComponent.cs`
- `Content.Shared/_Mythos/Magic/Mana/SharedManaSystem.cs`
- `Content.Shared/_Mythos/Magic/MagicMissile/SharedMagicMissileSystem.cs`
- `Content.Server/_Mythos/Magic/MagicMissile/MagicMissileSystem.cs`
- `Content.Shared/_Mythos/Magic/Fireball/SharedFireballSystem.cs`
- `Content.Server/_Mythos/Magic/Fireball/FireballSystem.cs`
- `Content.Shared/_Mythos/Magic/Actions/SpellActionEvents.cs`
- `Content.Server/_Mythos/Magic/Actions/SpellActionsSystem.cs`
- `Content.Server/_Mythos/Player/MythosSpellLoadoutSystem.cs`
- `Resources/Prototypes/_Mythos/Actions/combat_spells.yml`

## Player Flow

1. Player attaches to a living mob.
2. `MythosSpellLoadoutSystem` grants `ManaComponent`, `ActionMythosMagicMissile`, `ActionMythosFireball`, and `MythosSpellLoadoutComponent`.
3. Player enters combat mode.
4. Left click on a valid target raises `SelectCombatTargetEvent` predictively.
5. `CombatTargetComponent.Target` replicates and the client reticle overlay draws a ring.
6. With an empty queue, `CombatAutoAttackSystem` fires stock `LightAttackEvent` predictively when in range and weapon cooldown is clear.
7. Activating a spell action enqueues `QueuedActionKind.MagicMissile` or `QueuedActionKind.Fireball`.
8. While the queue is non-empty, auto-attack yields and `CombatQueueExecutor` owns the next fire.

## Targeting

Valid targets are:

- not self
- living mob with `MobStateComponent`, or any damageable non-mob with `DamageableComponent`
- not dead if it is a mob

The target system deliberately does not check hostility yet. Allies and neutral damageable entities are selectable. Add faction/harm-intent filtering in `SharedCombatTargetSystem.IsValidTarget` when that design exists.

## Auto-Attack

Auto-attack is client-driven and predictive. It raises the stock melee `LightAttackEvent`; the server applies damage through existing melee systems and other clients see the result through normal networking.

Important gate order in `CombatAutoAttackSystem`:

1. local player exists
2. `CombatTargetComponent.Target` exists
3. queue is empty
4. target exists and is alive
5. combat mode is enabled
6. melee weapon exists
7. weapon cooldown elapsed
8. user and target are on same map
9. target is within weapon range

Do not move this loop to the server without also solving local lunge prediction. The stock melee lunge path excludes the owner from server fan-out because it assumes the owner predicted the swing.

## Combat Queue

`CombatQueueComponent` stores up to five queued actions.

Key fields:

- `Queue`: FIFO list of `QueuedAction`
- `NextSlotId`: monotonic slot ID counter
- `CastingSlot`: non-null while a cast-time spell is in flight
- `CastingTarget`: target captured at cast start
- `NextActionAt`: shared global cooldown timestamp

Queue mutation methods live in `SharedCombatQueueSystem`:

- `TryEnqueue`
- `CancelSlot`
- `ClearQueue`
- `PopHead`
- `SetCastingSlot`
- `SetCastingTarget`
- `BumpGlobalCooldown`
- `IsOnGlobalCooldown`

Slot IDs are monotonic and never reused. This is important for prediction reconciliation: stale cancel/pop events no-op instead of editing the wrong queue entry.

## Mana

`ManaComponent` is a lazy resource pool:

- default max: `100`
- default current: `100`
- default regen: `4` mana per second
- default regen delay: `3` seconds after spend

Do not read `ManaComponent.Current` for display. Call:

```csharp
var mana = EntityManager.System<SharedManaSystem>();
var effective = mana.GetEffectiveMana(player);
```

Pure math helper:

```csharp
var effective = SharedManaSystem.CalculateEffectiveMana(comp, now);
```

Spend and refund only through `SharedManaSystem`:

```csharp
if (!_mana.TrySpend(caster, SharedFireballSystem.ManaCost))
    return false;

_mana.Refund(caster, SharedFireballSystem.ManaCost);
```

## Magic Missile

Magic Missile is the instant-cast proof of concept.

- Queue kind: `QueuedActionKind.MagicMissile`
- Action prototype: `ActionMythosMagicMissile`
- Event: `CastMagicMissileEvent`
- Cost: `SharedMagicMissileSystem.ManaCost`, currently `10`
- Damage: server applies `8 Slash`

Execution path:

1. Action hotbar event is handled by `SpellActionsSystem`.
2. Queue receives a `MagicMissile` entry.
3. Client executor sees queue head, target, mana, and cooldown are valid.
4. Client raises `CastMagicMissileEvent` predictively.
5. Shared handler spends mana on both sides.
6. Server override applies damage.
7. Executor raises `BumpGlobalCooldownEvent` and `PopQueueHeadEvent`.

## Fireball

Fireball is the cast-time proof of concept.

- Queue kind: `QueuedActionKind.Fireball`
- Action prototype: `ActionMythosFireball`
- Request event: `StartCastRequestEvent`
- DoAfter event: `FireballDoAfterEvent`
- Cost: `SharedFireballSystem.ManaCost`, currently `20`
- Cast time: `1.5` seconds
- Damage: server applies `15 Heat`
- Breaks on damage, not movement

Execution path:

1. Action hotbar event enqueues Fireball.
2. Client executor validates target and mana.
3. Client sends `StartCastRequestEvent`.
4. Server validates queue head, slot, kind, and mana.
5. Server spends mana and starts `SharedDoAfterSystem`.
6. Server sets `CastingSlot` and `CastingTarget`.
7. While `CastingSlot` is set, executor does not fire another action.
8. On completion, server clears casting fields, removes the queue entry, and applies damage to the captured target.
9. On cancellation, server clears casting fields, refunds mana, and leaves the queue entry intact.

## HUD

HUD is overlay-based to avoid upstream XAML edits:

- `ManaHudOverlay`: bottom-right mana bar, derives effective mana each frame.
- `CombatQueueHudOverlay`: queue row below the mana bar, draws one tile per queued action.
- `TargetReticleOverlay`: world-space red ring around selected target.

This is intentionally lightweight. A later UI pass can replace overlays with XAML widgets once the HUD layout is deliberately forked.

## Adding a New Spell

Minimum path for a queue-driven spell:

1. Add a `QueuedActionKind` value in `QueuedAction.cs`.
2. Add a shared event in `Content.Shared/_Mythos/Combat/Events/`.
3. Add or extend a shared spell system for prediction-visible resource logic.
4. Add server spell system logic for damage, status, spawn, or DoAfter lifecycle.
5. Add client shell or visual-only client behavior if needed.
6. Add an action event in `SpellActionEvents.cs`.
7. Add server bridge in `SpellActionsSystem`.
8. Add an action prototype under `Resources/Prototypes/_Mythos/Actions/`.
9. Grant it through loadout, item action, spellbook, or class system.
10. Add unit tests for pure/default contracts and integration tests for server flow.

Prefer moving costs and cast times into `SpellComponent` once multiple spells need tuning. Current hardcoded constants are acceptable because Magic Missile and Fireball are proof-of-concept spells.

## Known Gaps

- `SpellComponent` exists but is not yet the source of truth for spell tuning.
- Damage uses existing `Slash` and `Heat`; proper fantasy damage types are not added yet.
- Combat approach/pathing is stubbed in `Content.Server/_Mythos/Combat/Approach/`.
- Queue UI has no click-to-cancel controls yet.
- Fireball location is captured but not used for splash or projectile behavior yet.
- Spell loadout grants every living player both spells; class, wand, or spellbook gating is future work.

## Agent Search Terms

```powershell
rg -n "CombatTarget|SelectCombatTarget|DeselectCombatTarget|TargetReticle|CombatAutoAttack" Content.Client\_Mythos Content.Server\_Mythos Content.Shared\_Mythos
rg -n "CombatQueue|QueuedActionKind|EnqueueAction|PopQueueHead|BumpGlobalCooldown|CombatQueueExecutor" Content.Client\_Mythos Content.Server\_Mythos Content.Shared\_Mythos
rg -n "ManaComponent|SharedManaSystem|TrySpend|Refund|CalculateEffectiveMana" Content.Client\_Mythos Content.Server\_Mythos Content.Shared\_Mythos Content.Tests\_Mythos Content.IntegrationTests\_Mythos
rg -n "MagicMissile|Fireball|StartCastRequest|FireballDoAfter|SpellActionsSystem|ActionMythos" Content.Client\_Mythos Content.Server\_Mythos Content.Shared\_Mythos Resources\Prototypes\_Mythos
```

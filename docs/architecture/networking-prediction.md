# Networking and Prediction

## Purpose

Use this page before adding replicated components, client-to-server events, predictive gameplay, or bound UI messages. Mythos combat and magic already rely on this model.

Official references:

- https://docs.spacestation14.com/en/ss14-by-example/basic-networking-and-you.html
- https://docs.spacestation14.com/en/ss14-by-example/prediction-guide.html
- https://docs.spacestation14.com/en/robust-toolbox/ecs.html

## Source Anchors

- `Content.Shared/_Mythos/Magic/Mana/ManaComponent.cs`
- `Content.Shared/_Mythos/Magic/Mana/SharedManaSystem.cs`
- `Content.Shared/_Mythos/Combat/Targeting/CombatTargetComponent.cs`
- `Content.Shared/_Mythos/Combat/Targeting/SharedCombatTargetSystem.cs`
- `Content.Shared/_Mythos/Combat/Queue/CombatQueueComponent.cs`
- `Content.Shared/_Mythos/Combat/Queue/SharedCombatQueueSystem.cs`
- `Content.Shared/_Mythos/Combat/Events/CombatEvents.cs`
- `Content.Shared/_Mythos/Combat/Events/CombatQueueEvents.cs`
- `Content.Shared/_Mythos/Combat/Events/CombatMagicEvents.cs`
- `Content.Client/_Mythos/Combat/Queue/CombatQueueExecutor.cs`
- `Content.Client/_Mythos/Combat/Targeting/CombatAutoAttackSystem.cs`
- `Content.Server/_Mythos/Magic/Fireball/FireballSystem.cs`

## Core Rules

Network-visible state belongs in `Content.Shared`. A networked component must be in a shared assembly and should normally use source-generated state:

```csharp
[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ExampleComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Value;
}
```

Mutate component state through a system method, compare before writing where practical, then call `Dirty(uid, comp)`.

```csharp
public void SetValue(EntityUid uid, int value, ExampleComponent? comp = null)
{
    if (!Resolve(uid, ref comp))
        return;

    if (comp.Value == value)
        return;

    comp.Value = value;
    Dirty(uid, comp);
}
```

Client input sent to the server should be an event in shared code and should carry `NetEntity` / `NetCoordinates`, not raw `EntityUid` / `EntityCoordinates`, when it crosses the network.

```csharp
[Serializable, NetSerializable]
public sealed class SelectCombatTargetEvent : EntityEventArgs
{
    public NetEntity Target;

    public SelectCombatTargetEvent(NetEntity target)
    {
        Target = target;
    }
}
```

Resolve network entity IDs on the receiving side:

```csharp
private void OnSelect(SelectCombatTargetEvent ev, EntitySessionEventArgs args)
{
    if (args.SenderSession.AttachedEntity is not { } user)
        return;

    if (!TryGetEntity(ev.Target, out var target))
        return;

    TrySetTarget(user, target.Value);
}
```

## Predictive Events

Use `RaisePredictiveEvent` for client actions that should mutate shared state immediately and then be corrected by server state if necessary.

Mythos uses this for:

- `SelectCombatTargetEvent`: client click updates `CombatTargetComponent` immediately.
- `EnqueueActionEvent`, `CancelQueuedActionEvent`, `ClearQueueEvent`, `PopQueueHeadEvent`: shared queue mutations can be optimistic.
- `CastMagicMissileEvent`: mana spend happens on both sides; server also applies damage.
- `BumpGlobalCooldownEvent`: queue fire rate updates immediately on both sides.

Do not use predictive events for work that must be server-authoritative from the start. Fireball uses `StartCastRequestEvent` because starting a `DoAfter`, reserving mana, and applying damage are server-side lifecycle work.

## Prediction Guards

Client update loops that fire predicted events should usually gate on first-time prediction:

```csharp
public override void Update(float frameTime)
{
    if (!_timing.IsFirstTimePredicted)
        return;

    // Raise predictive events here.
}
```

This avoids repeated audiovisual effects or repeated event sends during prediction replay. It does not fix incorrect state logic. If prediction diverges, fix the shared mutation path or the data being dirtied.

## Existing Mythos Patterns

### Lazy Mana

`ManaComponent` networks an anchor state: `Current`, `LastUpdate`, `NextRegenTime`, `Max`, and `RegenPerSecond`. It does not dirty every tick. Both client and server call `SharedManaSystem.CalculateEffectiveMana` to derive current mana from the same anchor.

Use this pattern for any resource that regenerates continuously:

- store the value at a known time
- store the next allowed regen time
- derive current value locally
- dirty only on spend, refund, or tuning mutation

### Combat Target

`CombatTargetComponent.Target` is replicated. `CombatTargetClickSystem` raises a predictive select event, `SharedCombatTargetSystem` validates, and server `CombatTargetSystem` clears invalid or dead targets.

This gives instant reticle feedback without making the client authoritative over damage.

### Combat Queue

`CombatQueueComponent.Queue` is replicated as a list of `QueuedAction`. Slot IDs are monotonic so stale pop/cancel events cannot accidentally target a recycled slot. Queue mutation lives in `SharedCombatQueueSystem`, so client and server run the same code for predictive queue operations.

The queue executor is client-side because stock melee lunge effects assume the owning client predicted its own attack. The server remains authoritative because damage systems still run server-side.

### Fireball DoAfter

`FireballSystem` is server-authoritative:

1. Client executor sends `StartCastRequestEvent`.
2. Server validates queue head and mana.
3. Server starts `SharedDoAfterSystem`.
4. `DoAfterComponent` state gives the client a progress bar.
5. Completion applies damage and removes the queue slot.
6. Cancellation refunds mana and leaves the queue slot intact.

## Bound UI Notes

For entity UIs, prefer component state as the replicated source of truth when the data already lives on a component. A predicted BUI can update from component state on the client instead of duplicating the same data in `BoundUserInterfaceState`.

Use `SendPredictedMessage` for predicted UI button messages. Use plain server messages for admin actions or non-predicted workflows.

## Common Failure Modes

- `NetworkedComponent` outside `Content.Shared`: it will not replicate correctly.
- Dirtying every tick for a regenerating value: use timestamp interpolation instead.
- Sending raw `EntityUid` in network events: use `NetEntity`.
- Trusting client events: always revalidate target, queue head, mana, range, and permissions on the server.
- Calling `Spawn` in shared predicted code: use predicted spawn helpers for predicted entities, or keep spawning server-side.
- Forgetting the client concrete system subclass for a shared base: DI needs instantiable client and server classes when both sides use the system.

## Agent Search Terms

```powershell
rg -n "NetworkedComponent|AutoGenerateComponentState|AutoNetworkedField|Dirty\\(" Content.Shared Content.Client Content.Server
rg -n "RaisePredictiveEvent|SubscribeAllEvent|EntitySessionEventArgs|NetEntity|GetNetEntity|TryGetEntity" Content.Client Content.Shared Content.Server
rg -n "IsFirstTimePredicted|ApplyingState|PredictedSpawn|SendPredictedMessage" Content.Client Content.Shared Content.Server RobustToolbox
rg -n "ManaComponent|CombatTargetComponent|CombatQueueComponent|StartCastRequestEvent|FireballDoAfterEvent" Content.Client\_Mythos Content.Server\_Mythos Content.Shared\_Mythos
```

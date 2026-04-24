using Content.Shared.Mythos.Combat.Queue;
using Robust.Shared.Serialization;

namespace Content.Shared.Mythos.Combat.Events;

/// <summary>
/// Client-to-server predictive intent: enqueue an action against (optionally)
/// a specific target. If <see cref="TargetOverride"/> is null, the action fires
/// against the player's current <c>CombatTargetComponent.Target</c> when it
/// dequeues. Raised via <c>RaisePredictiveEvent</c> so the shared handler runs
/// on both sides and the queue widget updates optimistically.
/// </summary>
[Serializable, NetSerializable]
public sealed class EnqueueActionEvent : EntityEventArgs
{
    public QueuedActionKind Kind;
    public NetEntity? TargetOverride;

    public EnqueueActionEvent(QueuedActionKind kind, NetEntity? targetOverride)
    {
        Kind = kind;
        TargetOverride = targetOverride;
    }
}

/// <summary>
/// Client-to-server predictive intent: cancel the queued action identified by
/// the given monotonic slot ID. No-op if no such slot exists (idempotent
/// under reconciliation).
/// </summary>
[Serializable, NetSerializable]
public sealed class CancelQueuedActionEvent : EntityEventArgs
{
    public ushort Slot;

    public CancelQueuedActionEvent(ushort slot)
    {
        Slot = slot;
    }
}

/// <summary>
/// Client-to-server predictive intent: clear the entire queue.
/// </summary>
[Serializable, NetSerializable]
public sealed class ClearQueueEvent : EntityEventArgs
{
}

/// <summary>
/// Raised by the client-side queue executor after firing the head action's
/// effect (e.g. <c>HeavyAttackEvent</c>) to advance the queue on both sides.
/// The expected-slot guard prevents an out-of-order pop from trimming the
/// wrong slot during state reconciliation after a misprediction.
/// </summary>
[Serializable, NetSerializable]
public sealed class PopQueueHeadEvent : EntityEventArgs
{
    public ushort ExpectedSlot;

    public PopQueueHeadEvent(ushort expectedSlot)
    {
        ExpectedSlot = expectedSlot;
    }
}

/// <summary>
/// Raised by the client-side queue executor after any successful queue-driven
/// fire (instant spell, Fireball cast start, or queued heavy-attack) so both
/// sides bump <c>CombatQueueComponent.NextActionAt</c> by the shared
/// <c>SharedCombatQueueSystem.GlobalCooldown</c>. Predictive so the local
/// executor sees the new cooldown immediately, preventing another fire in
/// the same tick while the state replicates server-side.
/// </summary>
[Serializable, NetSerializable]
public sealed class BumpGlobalCooldownEvent : EntityEventArgs
{
}

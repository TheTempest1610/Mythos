using Robust.Shared.Serialization;

namespace Content.Shared.Mythos.Combat.Queue;

/// <summary>
/// A single action intent in a player's combat queue. Stored by-value inside
/// <see cref="CombatQueueComponent.Queue"/> and networked as part of that
/// component's replicated state.
///
/// Slot IDs are monotonic within a component lifetime: cancelling slot N does
/// not recycle N; the next enqueue gets N+1. Client and server match their
/// views by slot ID, not list index, so concurrent cancels and enqueues don't
/// race during state reconciliation.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class QueuedAction
{
    [DataField]
    public QueuedActionKind Kind;

    /// <summary>
    /// Explicit target override. When null, the action fires against the
    /// player's current <c>CombatTargetComponent.Target</c> at dequeue time.
    /// Reserved for friendly-target spells (Cure Wounds, Bless, etc.).
    /// </summary>
    [DataField]
    public NetEntity? TargetOverride;

    [DataField]
    public ushort Slot;

    public QueuedAction()
    {
    }

    public QueuedAction(QueuedActionKind kind, NetEntity? targetOverride, ushort slot)
    {
        Kind = kind;
        TargetOverride = targetOverride;
        Slot = slot;
    }
}

/// <summary>
/// Discriminator for actions the combat queue can execute.
/// <see cref="HeavyAttack"/> is the melee proof-of-concept;
/// <see cref="MagicMissile"/> is the instant-spell proof-of-concept;
/// <see cref="Fireball"/> is the cast-time-spell proof-of-concept. Future
/// work extends this with pluggable action-entity references once the
/// Actions system is integrated.
/// </summary>
[Serializable, NetSerializable]
public enum QueuedActionKind : byte
{
    HeavyAttack,
    MagicMissile,
    Fireball,
}

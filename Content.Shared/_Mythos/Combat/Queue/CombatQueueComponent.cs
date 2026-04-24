using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Mythos.Combat.Queue;

/// <summary>
/// Queue of pending combat actions for a player. Actions drain in FIFO order
/// via the client-side <c>CombatQueueExecutor</c>, which observes this component
/// each tick and fires the head's corresponding action event when the player is
/// ready (target valid, cooldown elapsed, in range).
///
/// Placing the execution loop client-side mirrors the auto-attack
/// architecture for the same reason: server-driven swings have their
/// <c>MeleeLungeEvent</c> filtered out of the owning user's PVS by the stock
/// <c>DoLunge(predicted=true)</c> default, so the player would see damage apply
/// but no animation. Client-driven predictive firing rides the stock melee
/// pipeline and the animation plays locally.
///
/// State mutation (enqueue / cancel / clear / pop-head) lives in
/// <see cref="SharedCombatQueueSystem"/> so that predictive events from the
/// client run the same handler here as the authoritative server, giving the
/// queue widget (future UX polish) optimistic latency while the server retains
/// the final word via component state replication.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
[AutoGenerateComponentPause]
[Access(typeof(SharedCombatQueueSystem))]
public sealed partial class CombatQueueComponent : Component
{
    /// <summary>
    /// Maximum queue depth. Enqueue attempts past this size are rejected.
    /// </summary>
    public const int MaxSlots = 5;

    [AutoNetworkedField]
    public List<QueuedAction> Queue = new();

    /// <summary>
    /// Monotonic slot counter. Incremented on every successful enqueue and
    /// never reused, so cancels and enqueues that race during reconciliation
    /// can still identify the correct slot by ID.
    /// </summary>
    [AutoNetworkedField]
    public ushort NextSlotId;

    /// <summary>
    /// Slot ID of the queue entry currently being channelled as a cast-time
    /// spell. Non-null means a DoAfter is in flight; the client
    /// executor skips until it clears. The server-side Fireball system sets
    /// this on successful cast start and clears it on completion or
    /// cancellation of the associated DoAfter.
    /// </summary>
    [AutoNetworkedField]
    public ushort? CastingSlot;

    /// <summary>
    /// Target entity the current cast is aimed at, captured at cast start so
    /// world-target spells (Fireball) still resolve a damage target even if
    /// the caster's <c>CombatTargetComponent.Target</c> changes or the
    /// original mob dies mid-cast. Null when no cast is in flight.
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? CastingTarget;

    /// <summary>
    /// Earliest time the queue executor is permitted to fire the next action.
    /// Pushed forward by <c>SharedCombatQueueSystem.GlobalCooldown</c> after
    /// every successful queue-driven fire (instant spell, Fireball cast start,
    /// or queued heavy-attack). Gates the executor's fire-rate so hotkey spam
    /// can't chain-cast the pool empty within a tick, even if the underlying
    /// per-action <c>useDelay</c> is zero.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextActionAt;
}

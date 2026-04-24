using Content.Shared.Mythos.Combat.Events;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Mythos.Combat.Queue;

/// <summary>
/// Shared owner of <see cref="CombatQueueComponent"/> state mutation. Handles
/// enqueue / cancel / clear / pop-head events and exposes the same operations
/// as a direct public API for programmatic use (and tests).
///
/// Lives in shared code so that predictive events from the client run the same
/// mutation path here as the authoritative server, giving the queue widget
/// optimistic latency while the server arbitrates via state replication.
/// Server wins on any divergence; the client's optimistic write is quietly
/// overwritten when the authoritative state tick arrives.
/// </summary>
public abstract class SharedCombatQueueSystem : EntitySystem
{
    /// <summary>
    /// Shared "global cooldown" applied between queue-driven action fires.
    /// Borrowed from classic MMO semantics: any queued ability (spell, cast
    /// start, heavy-attack) pushes out the next permitted fire. Auto-attack
    /// stays independent so melee DPS isn't gated by spell casts.
    /// </summary>
    public static readonly TimeSpan GlobalCooldown = TimeSpan.FromSeconds(2);

    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<EnqueueActionEvent>(OnEnqueue);
        SubscribeAllEvent<CancelQueuedActionEvent>(OnCancel);
        SubscribeAllEvent<ClearQueueEvent>(OnClear);
        SubscribeAllEvent<PopQueueHeadEvent>(OnPopHead);
        SubscribeAllEvent<BumpGlobalCooldownEvent>(OnBumpGlobalCooldown);
    }

    private void OnBumpGlobalCooldown(BumpGlobalCooldownEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        BumpGlobalCooldown(user);
    }

    private void OnEnqueue(EnqueueActionEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        TryEnqueue(user, ev.Kind, ev.TargetOverride);
    }

    private void OnCancel(CancelQueuedActionEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        CancelSlot(user, ev.Slot);
    }

    private void OnClear(ClearQueueEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        ClearQueue(user);
    }

    private void OnPopHead(PopQueueHeadEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        PopHead(user, ev.ExpectedSlot);
    }

    /// <summary>
    /// Appends a new action to the queue tail. Returns false and no-ops when
    /// the queue is already at <see cref="CombatQueueComponent.MaxSlots"/>
    /// capacity. On success the action is assigned the next monotonic slot ID.
    /// </summary>
    public bool TryEnqueue(EntityUid user, QueuedActionKind kind, NetEntity? targetOverride)
    {
        var comp = EnsureComp<CombatQueueComponent>(user);
        if (comp.Queue.Count >= CombatQueueComponent.MaxSlots)
            return false;

        var slot = comp.NextSlotId++;
        comp.Queue.Add(new QueuedAction(kind, targetOverride, slot));
        Dirty(user, comp);
        return true;
    }

    /// <summary>
    /// Removes the queued action with the given slot ID. Slots earlier in the
    /// queue keep their IDs; only the matching entry is pulled out. Returns
    /// false if no slot with that ID exists.
    /// </summary>
    public bool CancelSlot(EntityUid user, ushort slot)
    {
        if (!TryComp<CombatQueueComponent>(user, out var comp))
            return false;

        for (var i = 0; i < comp.Queue.Count; i++)
        {
            if (comp.Queue[i].Slot != slot)
                continue;

            comp.Queue.RemoveAt(i);
            Dirty(user, comp);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Empties the queue. No-op when already empty. Does not reset
    /// <see cref="CombatQueueComponent.NextSlotId"/>; slot IDs remain
    /// monotonic across clears so any in-flight events targeting old slots
    /// are still rejected rather than matching a recycled ID.
    /// </summary>
    public void ClearQueue(EntityUid user)
    {
        if (!TryComp<CombatQueueComponent>(user, out var comp) || comp.Queue.Count == 0)
            return;

        comp.Queue.Clear();
        Dirty(user, comp);
    }

    /// <summary>
    /// Removes the queue head if and only if its slot ID matches
    /// <paramref name="expectedSlot"/>. The guard prevents out-of-order pops
    /// during reconciliation: if the client predicted a pop but the server's
    /// authoritative state has a different head, the stale pop no-ops and the
    /// server's state prevails on the next replication.
    /// </summary>
    public bool PopHead(EntityUid user, ushort expectedSlot)
    {
        if (!TryComp<CombatQueueComponent>(user, out var comp) || comp.Queue.Count == 0)
            return false;

        if (comp.Queue[0].Slot != expectedSlot)
            return false;

        comp.Queue.RemoveAt(0);
        Dirty(user, comp);
        return true;
    }

    /// <summary>
    /// Sets the currently-channelling slot indicator. Null clears it. Used by
    /// the cast-time spell system to gate executor ticks while a DoAfter is
    /// in flight; centralised here so the <c>[Access]</c> discipline holds:
    /// only this system (and derived server/client subclasses) writes to
    /// the component.
    /// </summary>
    public void SetCastingSlot(EntityUid user, ushort? slot)
    {
        if (!TryComp<CombatQueueComponent>(user, out var comp))
            return;

        if (comp.CastingSlot == slot)
            return;

        comp.CastingSlot = slot;
        Dirty(user, comp);
    }

    /// <summary>
    /// Sets the target entity the current cast is aimed at. Null clears it.
    /// Paired with <see cref="SetCastingSlot"/>: set on cast start, clear
    /// alongside the slot when the DoAfter finishes either way.
    /// </summary>
    public void SetCastingTarget(EntityUid user, EntityUid? target)
    {
        if (!TryComp<CombatQueueComponent>(user, out var comp))
            return;

        if (comp.CastingTarget == target)
            return;

        comp.CastingTarget = target;
        Dirty(user, comp);
    }

    /// <summary>
    /// Pushes the queue's <c>NextActionAt</c> out by <see cref="GlobalCooldown"/>
    /// from the current game time. Called after any successful queue-driven
    /// fire to rate-limit the executor. The queue component is ensured so
    /// first-fire scenarios don't need the component to pre-exist.
    /// </summary>
    public void BumpGlobalCooldown(EntityUid user)
    {
        var comp = EnsureComp<CombatQueueComponent>(user);
        comp.NextActionAt = _timing.CurTime + GlobalCooldown;
        Dirty(user, comp);
    }

    /// <summary>
    /// Returns true if the global-cooldown window is still active for this
    /// user at the current game time.
    /// </summary>
    public bool IsOnGlobalCooldown(EntityUid user)
    {
        if (!TryComp<CombatQueueComponent>(user, out var comp))
            return false;

        return comp.NextActionAt > _timing.CurTime;
    }
}

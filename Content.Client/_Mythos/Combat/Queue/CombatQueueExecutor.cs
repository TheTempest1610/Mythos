using System.Collections.Generic;
using Content.Shared.CombatMode;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mythos.Combat.Events;
using Content.Shared.Mythos.Combat.Queue;
using Content.Shared.Mythos.Combat.Targeting;
using Content.Shared.Mythos.Magic.Fireball;
using Content.Shared.Mythos.Magic.Mana;
using Content.Shared.Mythos.Magic.MagicMissile;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Client.Mythos.Combat.Queue;

/// <summary>
/// Client-side executor for the combat queue. Each tick, dispatches on the
/// head action's <see cref="QueuedActionKind"/> and fires the matching
/// predictive effect when that kind's preconditions are met. On successful
/// fire, raises a <see cref="PopQueueHeadEvent"/> to advance the queue on
/// both sides.
///
/// Per-kind gates:
/// <list type="bullet">
///   <item><c>HeavyAttack</c>: combat mode on, weapon available, weapon
///     cooldown elapsed, target in melee range. Fires
///     <c>HeavyAttackEvent</c> through the stock melee pipeline so the
///     lunge animation plays locally.</item>
///   <item><c>MagicMissile</c>: combat mode on, <see cref="ManaComponent"/>
///     present with sufficient effective mana. Fires
///     <c>CastMagicMissileEvent</c>; mana is actually spent inside the
///     shared handler, on both sides, so the mana bar updates
///     optimistically.</item>
/// </list>
///
/// The plan stipulated "queue rejects on OOM at dequeue time, not enqueue."
/// That's implemented here: an insufficient-mana magic-missile head simply
/// returns without firing or popping, so the queue stalls until regen has
/// caught up rather than dropping the action.
/// </summary>
public sealed class CombatQueueExecutor : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SharedCombatQueueSystem _queue = default!;
    [Dependency] private readonly SharedManaSystem _mana = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    /// <summary>
    /// Highest monotonic slot ID this executor has already fired for the
    /// local player. Since slot IDs never recycle (see
    /// <c>SharedCombatQueueSystem.TryEnqueue</c>), this is the only check
    /// we need to prevent re-fires caused by state-replication lag:
    /// after the executor raises a cast + pop pair, the server's
    /// <c>CombatQueueComponent</c> can still reflect the pre-pop state for
    /// a tick or two, and the head locally rolls back to the already-fired
    /// slot. Without this guard, each of those ticks would fire the spell
    /// again and drain the whole mana pool from a single hotkey press.
    ///
    /// Reset to null when the local entity changes (e.g. ghosting) so a
    /// fresh entity starts with no fired history.
    /// </summary>
    private ushort? _highestFiredSlot;
    private EntityUid? _lastSeenLocal;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        if (_player.LocalEntity is not { } user)
            return;

        // Reset the fired-slot marker when the player takes control of a
        // different entity. Prevents stale markers from an earlier mob
        // suppressing casts on a new one.
        if (_lastSeenLocal != user)
        {
            _highestFiredSlot = null;
            _lastSeenLocal = user;
        }

        if (!TryComp<CombatQueueComponent>(user, out var queue) || queue.Queue.Count == 0)
        {
            // Empty queue is a good moment to reset the tracker too;
            // guards against ushort wraparound on very long sessions.
            _highestFiredSlot = null;
            return;
        }

        // A cast-time spell is in flight; let its DoAfter run. The server
        // clears CastingSlot on completion or cancellation.
        if (queue.CastingSlot != null)
            return;

        // Global cooldown: any prior queue fire (spell or cast start)
        // rate-limits the next one. Gates the executor even when the
        // per-action useDelay is zero, so a Magic-Missile rotation can't
        // chain-fire within a single tick.
        if (_queue.IsOnGlobalCooldown(user))
            return;

        var head = queue.Queue[0];

        // Already fired this (or an earlier) slot. Monotonic IDs mean any
        // slot <= _highestFiredSlot has been dispatched even if the network
        // hasn't yet caught up enough to pop it from our local view.
        if (_highestFiredSlot is { } fired && head.Slot <= fired)
            return;

        if (!_combatMode.IsInCombatMode(user))
            return;

        var target = ResolveTarget(user, head);
        if (target is not { } targetUid)
            return;

        if (!Exists(targetUid) || _mobState.IsDead(targetUid))
            return;

        // Fireball is server-authoritative; it does not pop the queue here.
        // Cast start is a request to the server, which pops on DoAfter finish.
        if (head.Kind == QueuedActionKind.Fireball)
        {
            if (TryStartFireball(user, targetUid, head.Slot))
            {
                _highestFiredSlot = head.Slot;
                RaisePredictiveEvent(new BumpGlobalCooldownEvent());
            }
            return;
        }

        var firedKind = head.Kind switch
        {
            QueuedActionKind.HeavyAttack => TryFireHeavyAttack(user, targetUid),
            QueuedActionKind.MagicMissile => TryFireMagicMissile(user, targetUid),
            _ => false,
        };

        if (!firedKind)
            return;

        _highestFiredSlot = head.Slot;
        RaisePredictiveEvent(new BumpGlobalCooldownEvent());
        RaisePredictiveEvent(new PopQueueHeadEvent(head.Slot));
    }

    private bool TryStartFireball(EntityUid user, EntityUid target, ushort slot)
    {
        // Pre-flight mana check so we don't round-trip a rejection the server
        // would also reject; the server revalidates for authority regardless.
        if (!TryComp<ManaComponent>(user, out var mana) ||
            _mana.GetEffectiveMana(user, mana) < SharedFireballSystem.ManaCost)
            return false;

        if (!TryComp(target, out TransformComponent? targetXform))
            return false;

        RaisePredictiveEvent(new StartCastRequestEvent(
            QueuedActionKind.Fireball,
            GetNetEntity(target),
            GetNetCoordinates(targetXform.Coordinates),
            slot));
        return true;
    }

    /// <summary>
    /// Resolves the entity a queued action fires against. Explicit
    /// <see cref="QueuedAction.TargetOverride"/> wins when set (used by
    /// friendly-target spells); otherwise falls back to the player's current
    /// <see cref="CombatTargetComponent.Target"/>.
    /// </summary>
    private EntityUid? ResolveTarget(EntityUid user, QueuedAction head)
    {
        if (head.TargetOverride is { } netTarget)
            return TryGetEntity(netTarget, out var overridden) ? overridden : null;

        if (!TryComp<CombatTargetComponent>(user, out var combatTarget))
            return null;

        return combatTarget.Target;
    }

    private bool TryFireHeavyAttack(EntityUid user, EntityUid target)
    {
        if (!_melee.TryGetWeapon(user, out var weaponUid, out var weapon))
            return false;

        if (weapon.NextAttack > _timing.CurTime)
            return false;

        if (!TryComp(user, out TransformComponent? userXform) ||
            !TryComp(target, out TransformComponent? targetXform))
            return false;

        if (userXform.MapID != targetXform.MapID)
            return false;

        var delta = _xform.GetWorldPosition(targetXform) - _xform.GetWorldPosition(userXform);
        if (delta.Length() > weapon.Range)
            return false;

        var ev = new HeavyAttackEvent(
            GetNetEntity(weaponUid),
            new List<NetEntity> { GetNetEntity(target) },
            GetNetCoordinates(targetXform.Coordinates));
        RaisePredictiveEvent(ev);
        return true;
    }

    private bool TryFireMagicMissile(EntityUid user, EntityUid target)
    {
        if (!TryComp<ManaComponent>(user, out var mana))
            return false;

        if (_mana.GetEffectiveMana(user, mana) < SharedMagicMissileSystem.ManaCost)
            return false;

        var ev = new CastMagicMissileEvent(GetNetEntity(target));
        RaisePredictiveEvent(ev);
        return true;
    }
}

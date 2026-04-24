using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Mythos.Combat.Events;
using Content.Shared.Mythos.Combat.Queue;
using Content.Shared.Mythos.Magic.Fireball;
using Content.Shared.Mythos.Magic.Mana;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Mythos.Magic.Fireball;

/// <summary>
/// Server-authoritative Fireball cast lifecycle. Owns the full sequence:
/// validate request → spend mana → start DoAfter → handle completion or
/// cancellation → apply damage or refund mana → clear cast flag.
///
/// DoAfter is chosen here rather than a custom timer because the stock
/// system already wires <see cref="DoAfterArgs.BreakOnDamage"/> and
/// <c>BreakOnMove</c> into the existing damage and movement event paths,
/// and because the replicated <c>DoAfterComponent</c> state produces a
/// cast-bar on the client with no extra client work. The Fireball-specific
/// <see cref="FireballDoAfterEvent"/> is the completion event and also the
/// duplicate-detection discriminator (<c>IsDuplicate</c> compares types),
/// so a second Fireball request while one is already casting is naturally
/// rejected by the DoAfter system.
///
/// Currently uses <c>Heat</c> as the damage type so the spell slots into the
/// existing resistance model without a new prototype; a later polish pass
/// should add a proper <c>Fire</c> / <c>Arcane</c> axis with retuned resistances.
/// </summary>
public sealed class FireballSystem : SharedFireballSystem
{
    private const string FireballDamageType = "Heat";

    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedCombatQueueSystem _queue = default!;
    [Dependency] private readonly SharedManaSystem _mana = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private DamageSpecifier? _damageSpec;

    public override void Initialize()
    {
        base.Initialize();
        BuildDamageSpec();
        SubscribeAllEvent<StartCastRequestEvent>(OnStartRequest);
        SubscribeLocalEvent<CombatQueueComponent, FireballDoAfterEvent>(OnCastFinished);
    }

    private void BuildDamageSpec()
    {
        if (!_proto.TryIndex<DamageTypePrototype>(FireballDamageType, out _))
            return;

        _damageSpec = new DamageSpecifier();
        _damageSpec.DamageDict[FireballDamageType] = FixedPoint2.New(Damage);
    }

    private void OnStartRequest(StartCastRequestEvent ev, EntitySessionEventArgs args)
    {
        if (ev.Kind != QueuedActionKind.Fireball)
            return;

        if (args.SenderSession.AttachedEntity is not { } caster)
            return;

        EntityUid? target = null;
        if (ev.Target is { } netTarget && TryGetEntity(netTarget, out var resolved))
            target = resolved.Value;

        TryStartFireball(caster, target, ev.Location, ev.Slot);
    }

    /// <summary>
    /// Starts a Fireball cast for <paramref name="caster"/>. Validates queue
    /// head, reserves mana, launches the DoAfter, and flags the queue with
    /// both slot and target. Public so integration tests can drive the flow
    /// without synthesising a session.
    /// </summary>
    public bool TryStartFireball(EntityUid caster, EntityUid? target, NetCoordinates location, ushort slot)
    {
        if (!TryComp<CombatQueueComponent>(caster, out var queue))
            return false;

        // Already channelling: reject silently. Covers both client misprediction
        // and DoAfter's own duplicate rejection path.
        if (queue.CastingSlot != null)
            return false;

        if (queue.Queue.Count == 0 || queue.Queue[0].Slot != slot || queue.Queue[0].Kind != QueuedActionKind.Fireball)
            return false;

        if (!_mana.TrySpend(caster, ManaCost))
            return false;

        var doAfterArgs = new DoAfterArgs(EntityManager, caster, CastTime, new FireballDoAfterEvent(), caster, target: caster)
        {
            BreakOnDamage = true,
            BreakOnMove = false,
            Broadcast = false,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
        {
            // Couldn't start; refund and bail.
            _mana.Refund(caster, ManaCost);
            return false;
        }

        _queue.SetCastingSlot(caster, slot);
        _queue.SetCastingTarget(caster, target);
        // Defence-in-depth: the client-side executor bumps the GCD on the
        // predictive event, but an authoritative server bump guarantees it
        // even if the client skipped the bump (unusual client, replay, etc.).
        _queue.BumpGlobalCooldown(caster);
        return true;
    }

    private void OnCastFinished(EntityUid caster, CombatQueueComponent queue, FireballDoAfterEvent args)
    {
        var slot = queue.CastingSlot;
        var target = queue.CastingTarget;
        _queue.SetCastingSlot(caster, null);
        _queue.SetCastingTarget(caster, null);

        if (args.Cancelled)
        {
            _mana.Refund(caster, ManaCost);
            return;
        }

        // Remove the completed entry from the queue. CancelSlot is a no-op if
        // the slot has already been removed (e.g., a concurrent user clear),
        // which keeps this handler robust against queue-edit races.
        if (slot is { } slotId)
            _queue.CancelSlot(caster, slotId);

        if (_damageSpec == null)
            return;

        // Apply damage to the target captured at cast start, not the caster.
        // The world-coordinate location is currently ignored (AOE / splash
        // damage comes later); a null or deleted target is a silent no-op
        // so the cast doesn't accidentally hit someone else.
        if (target is not { } targetUid)
            return;

        if (!Exists(targetUid))
            return;

        _damage.TryChangeDamage(targetUid, _damageSpec, origin: caster);
    }
}

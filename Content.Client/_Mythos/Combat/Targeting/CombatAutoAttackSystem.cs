using Content.Shared.CombatMode;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mythos.Combat.Queue;
using Content.Shared.Mythos.Combat.Targeting;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Client.Mythos.Combat.Targeting;

/// <summary>
/// Client-side auto-attack loop against the selected combat target. Each tick, if
/// the local player has a valid combat target within weapon range, combat mode is
/// on, and the weapon's cooldown has elapsed, this raises a predictive
/// <see cref="LightAttackEvent"/>, running the stock melee pipeline on both the
/// client (for local animation and cooldown advancement) and the server (for
/// damage and PVS fan-out to other clients). No upstream edits needed.
///
/// Placing the auto-attack loop client-side is the correct home: the stock
/// <c>MeleeWeaponSystem.DoLunge</c> defaults to <c>predicted=true</c>, which excludes
/// the local user from the server-broadcast lunge event on the assumption that
/// they predicted the swing themselves. A server-driven auto-attack would
/// therefore produce damage but no animation for the owning player.
/// </summary>
public sealed class CombatAutoAttackSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        if (_player.LocalEntity is not { } user)
            return;

        if (!TryComp<CombatTargetComponent>(user, out var comp))
            return;

        if (comp.Target is not { } target)
            return;

        // Yield to the combat queue. When the player has queued specials/spells,
        // the queue executor drives what fires next; auto-attack is only the
        // default behavior when the queue is empty.
        if (TryComp<CombatQueueComponent>(user, out var queue) && queue.Queue.Count > 0)
            return;

        if (!Exists(target) || _mobState.IsDead(target))
            return;

        if (!_combatMode.IsInCombatMode(user))
            return;

        if (!_melee.TryGetWeapon(user, out var weaponUid, out var weapon))
            return;

        if (weapon.NextAttack > _timing.CurTime)
            return;

        if (!TryComp(user, out TransformComponent? userXform) ||
            !TryComp(target, out TransformComponent? targetXform))
            return;

        if (userXform.MapID != targetXform.MapID)
            return;

        var delta = _xform.GetWorldPosition(targetXform) - _xform.GetWorldPosition(userXform);
        if (delta.Length() > weapon.Range)
            return;

        var ev = new LightAttackEvent(
            GetNetEntity(target),
            GetNetEntity(weaponUid),
            GetNetCoordinates(targetXform.Coordinates));
        RaisePredictiveEvent(ev);
    }
}

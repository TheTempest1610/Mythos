using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mythos.Combat.Events;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Mythos.Combat.Targeting;

/// <summary>
/// Shared owner of <see cref="CombatTargetComponent"/>. Handles the select/deselect
/// event contract and the public <see cref="TrySetTarget"/> / <see cref="ClearTarget"/>
/// entry points. Lives in shared code so that predictive events from the client run
/// the same mutation path here as the authoritative server, giving the target
/// reticle optimistic latency: the component is updated client-side the same tick
/// the click is raised, and the server's state replication arrives later to confirm
/// (or correct on the rare misprediction).
/// </summary>
public abstract class SharedCombatTargetSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly MobStateSystem MobState = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<SelectCombatTargetEvent>(OnSelect);
        SubscribeAllEvent<DeselectCombatTargetEvent>(OnDeselect);
    }

    private void OnSelect(SelectCombatTargetEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        if (!TryGetEntity(ev.Target, out var target))
            return;

        TrySetTarget(user, target.Value);
    }

    private void OnDeselect(DeselectCombatTargetEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        ClearTarget(user);
    }

    /// <summary>
    /// Returns true if <paramref name="target"/> is a legitimate combat target for
    /// <paramref name="user"/>. Must be distinct from the user and either a living
    /// mob or a damageable non-mob (walls, doors, crates: things a player can
    /// attack). Mobs that have died are rejected so auto-attack doesn't lock onto
    /// corpses.
    ///
    /// The union predicate (mob OR damageable) widens an earlier mob-only gate so
    /// that human players (who have both <see cref="MobStateComponent"/> and
    /// <see cref="DamageableComponent"/>) and structures (which have only
    /// <c>DamageableComponent</c>) are both selectable. Faction / hostility
    /// filtering is deliberately still absent; any valid damageable entity
    /// qualifies, including allies. Harm-intent refinement is later polish.
    /// </summary>
    public bool IsValidTarget(EntityUid user, EntityUid target)
    {
        if (user == target)
            return false;

        var hasMobState = HasComp<MobStateComponent>(target);
        var hasDamageable = HasComp<DamageableComponent>(target);

        if (!hasMobState && !hasDamageable)
            return false;

        if (hasMobState && MobState.IsDead(target))
            return false;

        return true;
    }

    /// <summary>
    /// Validates and, on success, stamps the user's <see cref="CombatTargetComponent"/>
    /// and dirties it for replication. Returns true if the target was accepted.
    /// Runs on both client (optimistic) and server (authoritative).
    /// </summary>
    public bool TrySetTarget(EntityUid user, EntityUid target)
    {
        if (!IsValidTarget(user, target))
            return false;

        var comp = EnsureComp<CombatTargetComponent>(user);
        comp.Target = target;
        comp.LastSeen = Timing.CurTime;
        Dirty(user, comp);
        return true;
    }

    /// <summary>
    /// Clears the user's combat target, if any. No-op if nothing to clear.
    /// Runs on both client (optimistic) and server (authoritative).
    /// </summary>
    public void ClearTarget(EntityUid user)
    {
        if (!TryComp<CombatTargetComponent>(user, out var comp) || comp.Target == null)
            return;

        comp.Target = null;
        Dirty(user, comp);
    }
}

using Content.Shared.Mythos.Combat.Targeting;

namespace Content.Server.Mythos.Combat.Targeting;

/// <summary>
/// Server-side combat target lifecycle maintenance. The select/deselect handlers and
/// the <c>TrySetTarget</c> / <c>ClearTarget</c> API live in the shared base so that
/// predictive client events mutate state identically on both sides. The server-only
/// responsibility here is the per-tick invariant loop: clear the combat target when
/// it dies or is deleted, so queued or in-progress auto-attacks stop chasing a ghost.
///
/// The auto-attack swings themselves are driven client-side via the stock
/// predictive-event pipeline (see <c>Content.Client/_Mythos/Combat/Targeting/CombatAutoAttackSystem.cs</c>),
/// so animations replicate through the normal PVS fan-out and the user's own client
/// sees its own swings predicted locally. Running the swing loop here instead would
/// leave the owning user without animation, since the server's <c>DoLunge</c> filter
/// excludes the user on the assumption that they predicted the swing.
/// </summary>
public sealed class CombatTargetSystem : SharedCombatTargetSystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CombatTargetComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Target is not { } target)
                continue;

            if (!Exists(target) || MobState.IsDead(target))
            {
                comp.Target = null;
                Dirty(uid, comp);
            }
        }
    }
}

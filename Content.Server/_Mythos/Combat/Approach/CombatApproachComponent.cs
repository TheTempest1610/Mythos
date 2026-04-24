namespace Content.Server.Mythos.Combat.Approach;

/// <summary>
/// Stub, not wired up yet. Declared here so the type exists for future
/// referrers and so the intended shape is pinned against the approved plan.
///
/// Transient marker attached to a player while their <c>CombatAutoAttackSystem</c>
/// needs auto-walk to reach the currently-selected combat target.
/// <see cref="CombatApproachSystem"/> will own the lifecycle: add on out-of-range
/// target, drive steering per tick, remove on arrival / target death / manual
/// movement input / stuck-for-N-ticks.
///
/// Server-only; not networked. The client only needs to know "auto-attack is
/// blocked by range," which it already infers from its own range check in
/// <c>Content.Client/_Mythos/Combat/Targeting/CombatAutoAttackSystem.cs</c>.
/// </summary>
[RegisterComponent]
public sealed partial class CombatApproachComponent : Component
{
    /// <summary>
    /// The entity the player is walking toward. Will be kept in sync
    /// with <c>CombatTargetComponent.Target</c>; if they diverge the approach
    /// should cancel and restart.
    /// </summary>
    [DataField]
    public EntityUid Target;

    /// <summary>
    /// Arrival tolerance: the approach system stops driving movement once the
    /// player is within this many tiles of the target. Defaults to the stock
    /// melee range so that auto-attack can engage immediately on arrival.
    /// </summary>
    [DataField]
    public float DesiredRange = 1.5f;
}

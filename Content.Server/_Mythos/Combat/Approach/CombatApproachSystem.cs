namespace Content.Server.Mythos.Combat.Approach;

/// <summary>
/// Stub, not wired up yet. Placeholder so the type exists for future
/// referrers and so registration reserves the DI slot now rather than introducing
/// it later.
///
/// Intended behavior: drives the player toward their selected combat
/// target when out of range. Lifecycle sketch from the approved plan and
/// research spike:
///
///   1. When <c>CombatAutoAttackSystem</c> reports out-of-range and target is
///      set, add <see cref="CombatApproachComponent"/> to the player.
///   2. Per tick for each <see cref="CombatApproachComponent"/>:
///      a. Request a path from
///         <c>Content.Server/NPC/Pathfinding/PathfindingSystem.GetPath(...)</c>
///         (entity-agnostic; no NPC components required on the player).
///      b. Consume the next <c>PathPoly</c> and write the desired direction
///         to the player's <c>InputMoverComponent.CurTickSprintMovement</c>,
///         setting <c>LastInputTick = _timing.CurTick</c> and
///         <c>LastInputSubTick = ushort.MaxValue</c> so physics consumes it.
///      c. If the player's manual movement input is non-zero this tick,
///         cancel the approach (remove the component); manual input wins.
///      d. If the target is dead / out-of-LOS for N ticks / the player has
///         not advanced for N ticks, cancel the approach.
///      e. If the player is now within
///         <c>CombatApproachComponent.DesiredRange</c> of the target, remove
///         the component, and <c>CombatAutoAttackSystem</c> picks up from there.
///
/// Research spike concluded that plugging the player directly into
/// <c>NPCSteeringSystem</c> is the wrong shape: the steering loop is gated on
/// <c>ActiveNPCComponent</c> and assumes HTN blackboard state that the Mythos
/// purge may have stripped. The plan of record is the standalone approach
/// above: call <c>PathfindingSystem</c> directly, drive <c>InputMoverComponent</c>
/// directly, no NPC-subsystem coupling.
/// </summary>
public sealed class CombatApproachSystem : EntitySystem
{
    // TODO: implement per the lifecycle sketch in the class XML doc.
    // Until then this system is inert: a registered-but-no-op EntitySystem
    // that costs nothing at runtime and keeps the type graph stable.
}

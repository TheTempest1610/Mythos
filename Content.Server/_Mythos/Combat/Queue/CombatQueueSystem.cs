using Content.Shared.Mythos.Combat.Queue;

namespace Content.Server.Mythos.Combat.Queue;

/// <summary>
/// Server side of the combat queue. All mutation logic (enqueue / cancel /
/// clear / pop-head) lives in <see cref="SharedCombatQueueSystem"/> so that
/// predictive client events can run the same handlers locally for optimistic
/// UI while the server stays authoritative via component state replication.
///
/// The queue head's actual execution runs on the client-side
/// <c>CombatQueueExecutor</c> (Content.Client/_Mythos/Combat/Queue/),
/// which raises the action's effect as a predictive event through the stock
/// melee pipeline. Driving execution from here would leave the owning user
/// without animation because the server's <c>DoLunge(predicted=true)</c>
/// default excludes them from the PVS broadcast; the same lesson that
/// shaped the auto-attack architecture.
/// </summary>
public sealed class CombatQueueSystem : SharedCombatQueueSystem
{
}

using Content.Shared.Mythos.Combat.Queue;

namespace Content.Client.Mythos.Combat.Queue;

/// <summary>
/// Client-side registration shell for the combat queue. All mutation logic
/// (enqueue / cancel / clear / pop-head / bump-GCD) lives in
/// <see cref="SharedCombatQueueSystem"/> so predictive events run the same
/// handlers on both sides. The client needs this concrete subclass to exist
/// because RobustToolbox's DI resolves systems by their instantiable
/// concrete type; an abstract base without a client-side subclass can't be
/// injected into the client-side <c>CombatQueueExecutor</c>.
/// </summary>
public sealed class CombatQueueSystem : SharedCombatQueueSystem
{
}

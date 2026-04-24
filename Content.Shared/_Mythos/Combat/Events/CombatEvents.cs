using Robust.Shared.Serialization;

namespace Content.Shared.Mythos.Combat.Events;

/// <summary>
/// Client-to-server predictive intent: player wants to select the given entity as
/// their combat target. Server validates (alive mob, not self) and updates the
/// player's <c>CombatTargetComponent</c> authoritatively.
/// </summary>
[Serializable, NetSerializable]
public sealed class SelectCombatTargetEvent : EntityEventArgs
{
    public NetEntity Target;

    public SelectCombatTargetEvent(NetEntity target)
    {
        Target = target;
    }
}

/// <summary>
/// Client-to-server predictive intent: player wants to clear their current combat
/// target.
/// </summary>
[Serializable, NetSerializable]
public sealed class DeselectCombatTargetEvent : EntityEventArgs
{
}

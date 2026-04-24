using Robust.Shared.GameStates;

namespace Content.Shared.Mythos.Combat.Targeting;

/// <summary>
/// Attached to a player (or any entity) that has selected a combat target. The server
/// auto-attack loop in <see cref="CombatTargetSystem"/> uses this to drive melee
/// swings against the target on weapon cooldown while in range.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(SharedCombatTargetSystem))]
public sealed partial class CombatTargetComponent : Component
{
    /// <summary>
    /// The currently-engaged target entity. Null when no target is selected.
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? Target;

    /// <summary>
    /// Last time the target was confirmed valid and visible. Stored for future
    /// LOS-timeout deselect logic; currently updated on select but not acted on.
    /// </summary>
    [AutoNetworkedField]
    public TimeSpan LastSeen;
}

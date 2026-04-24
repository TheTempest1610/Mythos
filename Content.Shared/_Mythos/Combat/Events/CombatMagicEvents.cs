using Content.Shared.Mythos.Combat.Queue;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Mythos.Combat.Events;

/// <summary>
/// Client-to-server predictive intent: cast Magic Missile at the given target.
/// Raised by the queue executor when a <c>MagicMissile</c> queue entry reaches
/// the head and the caster has sufficient mana. Both sides' handlers spend
/// the mana cost; the server additionally applies damage.
/// </summary>
[Serializable, NetSerializable]
public sealed class CastMagicMissileEvent : EntityEventArgs
{
    public NetEntity Target;

    public CastMagicMissileEvent(NetEntity target)
    {
        Target = target;
    }
}

/// <summary>
/// Client-to-server request: begin a cast-time spell. Carries both the
/// target entity (for entity-target damage) and the world location (for
/// future AOE / projectile landing logic), captured at cast start so the
/// outcome stays stable even if the caster's combat target changes or the
/// mob dies mid-cast. Does not run predictively: starting a DoAfter and
/// the associated mana reservation is server-authoritative. The
/// cast-in-flight indicator replicates back to the client via
/// <c>CombatQueueComponent.CastingSlot</c> / <c>CastingTarget</c>.
/// </summary>
[Serializable, NetSerializable]
public sealed class StartCastRequestEvent : EntityEventArgs
{
    public QueuedActionKind Kind;
    public NetEntity? Target;
    public NetCoordinates Location;
    public ushort Slot;

    public StartCastRequestEvent(QueuedActionKind kind, NetEntity? target, NetCoordinates location, ushort slot)
    {
        Kind = kind;
        Target = target;
        Location = location;
        Slot = slot;
    }
}

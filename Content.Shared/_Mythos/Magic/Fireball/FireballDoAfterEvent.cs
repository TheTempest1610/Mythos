using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Mythos.Magic.Fireball;

/// <summary>
/// DoAfter completion / cancellation event for Fireball. Raised by
/// <c>SharedDoAfterSystem</c> on the caster entity when the cast finishes
/// (successfully or via interruption; <c>args.Cancelled</c> discriminates).
/// The Fireball system's subscriber looks the cast context up off the
/// caster's <c>CombatQueueComponent.CastingSlot</c>, so the event itself
/// carries no payload; <see cref="SimpleDoAfterEvent"/> is sufficient.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class FireballDoAfterEvent : SimpleDoAfterEvent
{
}

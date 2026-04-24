using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Mythos.Magic.Mana;

/// <summary>
/// Magical resource pool for spellcasting. Lazy-evaluated: the stored
/// <see cref="Current"/> holds mana value at the time of the last mutation
/// (<see cref="LastUpdate"/>), and <c>SharedManaSystem.GetEffectiveMana</c>
/// interpolates regen forward from that anchor. Network dirty traffic only
/// happens on spend events; regen ticks are derived by both sides
/// identically from the same anchor, so client and server stay converged
/// without per-tick replication.
///
/// Modelled on the shape of <c>Content.Shared/Damage/Components/StaminaComponent.cs</c>
/// but with inverted polarity (mana starts full, drains on spend, regens up)
/// and a lazy-interpolation formula instead of a per-tick decay loop.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState(true)]
[AutoGenerateComponentPause]
public sealed partial class ManaComponent : Component
{
    /// <summary>
    /// Maximum mana pool. Sensible-defaulted; wand prototypes can raise this
    /// when granted to a caster.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Max = 100f;

    /// <summary>
    /// Mana value at the moment of <see cref="LastUpdate"/>. Do not read
    /// directly to display current mana; call
    /// <c>SharedManaSystem.GetEffectiveMana</c> so the lazy regen is applied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Current = 100f;

    /// <summary>
    /// Mana per second during active regen (i.e. once
    /// <see cref="NextRegenTime"/> has elapsed after the last spend).
    /// Tuned to 4 mana/s so sustained Magic Missile rotation (10 MP / 2 s
    /// global cooldown = 5 MP/s consumption) stays net-positive without the
    /// pool ever draining empty during normal play.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RegenPerSecond = 4f;

    /// <summary>
    /// Post-spend grace period before regen resumes. A spend always pushes
    /// <see cref="NextRegenTime"/> forward by this duration.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan RegenDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Timestamp when <see cref="Current"/> was last written (i.e. the last
    /// successful spend). The lazy-regen formula computes effective mana as
    /// <c>min(Max, Current + elapsed_since(max(LastUpdate, NextRegenTime)) * RegenPerSecond)</c>.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan LastUpdate;

    /// <summary>
    /// Earliest time regen is allowed to contribute to effective mana.
    /// Updated on every successful spend to <c>now + RegenDelay</c>.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextRegenTime;
}

namespace Content.Shared.Mythos.Magic.Spell;

/// <summary>
/// Metadata component for spell action entities. Declares the shape so
/// YAML prototypes can begin referencing it; cast-time spells wire it into
/// the queue executor's per-action gating once the Actions system
/// integrates with the combat queue.
///
/// Intentionally not networked: every side loads the same YAML, no
/// runtime mutation expected.
/// </summary>
[RegisterComponent]
public sealed partial class SpellComponent : Component
{
    /// <summary>
    /// Mana cost debited at cast time, consumed via
    /// <c>SharedManaSystem.TrySpend</c>.
    /// </summary>
    [DataField]
    public float ManaCost = 10f;

    /// <summary>
    /// Cast-bar duration. Zero means instant. Wired into
    /// <c>SharedDoAfterSystem</c> so casts can be interrupted.
    /// </summary>
    [DataField]
    public TimeSpan CastTime = TimeSpan.Zero;

    /// <summary>
    /// Per-action cooldown applied after a successful cast.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.Zero;

    /// <summary>
    /// Whether taking damage during the cast aborts it (via DoAfter
    /// integration).
    /// </summary>
    [DataField]
    public bool BreakOnDamage = true;

    /// <summary>
    /// Whether moving during the cast aborts it.
    /// </summary>
    [DataField]
    public bool BreakOnMove;

    /// <summary>
    /// How the spell's target is resolved at cast time.
    /// </summary>
    [DataField]
    public SpellTargetingKind Targeting = SpellTargetingKind.Entity;
}

public enum SpellTargetingKind : byte
{
    Self,
    Entity,
    World,
}

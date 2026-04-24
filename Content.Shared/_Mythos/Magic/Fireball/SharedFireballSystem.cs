namespace Content.Shared.Mythos.Magic.Fireball;

/// <summary>
/// Shared home for Fireball's tuning constants so both the client-side
/// <c>CombatQueueExecutor</c> (for its pre-dequeue mana check) and the
/// server-side <c>FireballSystem</c> agree on the cost / cast time /
/// damage without duplicating magic numbers across assemblies.
///
/// The data is hardcoded here for now while <c>SpellComponent</c>-driven
/// per-prototype tuning remains forward infrastructure. A later
/// Actions-system integration / damage-type polish pass will migrate
/// these to YAML prototype fields.
/// </summary>
public abstract class SharedFireballSystem : EntitySystem
{
    /// <summary>Mana deducted at cast start. Refunded in full on interrupt.</summary>
    public const float ManaCost = 20f;

    /// <summary>DoAfter duration. Interrupt triggers (damage) watch this window.</summary>
    public static readonly TimeSpan CastTime = TimeSpan.FromSeconds(1.5);

    /// <summary>Damage applied to the target on successful completion.</summary>
    public const int Damage = 15;
}

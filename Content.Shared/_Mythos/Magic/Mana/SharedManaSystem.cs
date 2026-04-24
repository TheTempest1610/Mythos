using Robust.Shared.Timing;

namespace Content.Shared.Mythos.Magic.Mana;

/// <summary>
/// Shared owner of <see cref="ManaComponent"/> math. Mana is stored lazily:
/// <c>Current</c> is the value at <c>LastUpdate</c>, and this system derives
/// the effective value at the current game time by interpolating regen
/// forward. Both client and server run the same formula against the same
/// anchor, so they stay in sync without per-tick dirty traffic.
///
/// The only write path is <see cref="TrySpend"/>, which runs on both sides
/// when a cast event is raised predictively, giving optimistic UI for the
/// mana bar (when added in a polish pass) while the server remains
/// authoritative via component state replication.
/// </summary>
public abstract class SharedManaSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;

    /// <summary>
    /// Returns mana available to the entity at the current game time. Lazily
    /// interpolates regen from the last anchor; returns zero if no
    /// <see cref="ManaComponent"/> is present.
    /// </summary>
    public float GetEffectiveMana(EntityUid uid, ManaComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return 0f;

        return CalculateEffectiveMana(comp, Timing.CurTime);
    }

    /// <summary>
    /// Pure function: given a component state snapshot and a query time,
    /// returns the effective mana value. Extracted so unit tests can exercise
    /// the regen math without spinning up the engine.
    /// </summary>
    public static float CalculateEffectiveMana(ManaComponent comp, TimeSpan now)
    {
        var regenStart = comp.LastUpdate > comp.NextRegenTime
            ? comp.LastUpdate
            : comp.NextRegenTime;

        if (now <= regenStart)
            return comp.Current;

        var elapsed = (float)(now - regenStart).TotalSeconds;
        var projected = comp.Current + elapsed * comp.RegenPerSecond;
        return projected > comp.Max ? comp.Max : projected;
    }

    /// <summary>
    /// Attempts to spend <paramref name="amount"/> mana. Rules:
    /// <list type="bullet">
    ///   <item>Negative amounts are rejected (returns false, no mutation).</item>
    ///   <item>Zero amount is a no-op that returns true.</item>
    ///   <item>Non-zero amount succeeds only if effective mana is sufficient;
    ///     on success, mana anchor is rewritten and <c>NextRegenTime</c> is
    ///     pushed out by <c>RegenDelay</c> from now.</item>
    /// </list>
    /// </summary>
    public bool TrySpend(EntityUid uid, float amount, ManaComponent? comp = null)
    {
        if (amount < 0f)
            return false;

        if (amount == 0f)
            return true;

        if (!Resolve(uid, ref comp, false))
            return false;

        var now = Timing.CurTime;
        var effective = CalculateEffectiveMana(comp, now);
        if (effective < amount)
            return false;

        comp.Current = effective - amount;
        comp.LastUpdate = now;
        comp.NextRegenTime = now + comp.RegenDelay;
        Dirty(uid, comp);
        return true;
    }

    /// <summary>
    /// Restores mana as if a prior spend had not happened. Called when a
    /// cast-time spell is interrupted and the reserved mana is refunded.
    /// Clamps to <see cref="ManaComponent.Max"/>, writes fresh anchors, and
    /// drops the regen delay so refund-then-regen is immediate rather than
    /// stacking with the now-invalidated post-spend cooldown. Negative amounts
    /// are rejected; zero is a no-op.
    /// </summary>
    public void Refund(EntityUid uid, float amount, ManaComponent? comp = null)
    {
        if (amount <= 0f)
            return;

        if (!Resolve(uid, ref comp, false))
            return;

        var now = Timing.CurTime;
        var effective = CalculateEffectiveMana(comp, now);
        var restored = effective + amount;
        comp.Current = restored > comp.Max ? comp.Max : restored;
        comp.LastUpdate = now;
        comp.NextRegenTime = now;
        Dirty(uid, comp);
    }
}

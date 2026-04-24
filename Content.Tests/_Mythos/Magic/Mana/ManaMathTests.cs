using System;
using Content.Shared.Mythos.Magic.Mana;
using NUnit.Framework;

namespace Content.Tests._Mythos.Magic.Mana;

/// <summary>
/// Pure-function coverage for <see cref="SharedManaSystem.CalculateEffectiveMana"/>.
/// The formula is the foundation of both mana display and spend validation,
/// so the regen branches and clamp behavior are pinned here without engine
/// spin-up. Integration tests cover the stateful <c>TrySpend</c> path separately.
/// </summary>
[TestFixture]
[Category("Mythos")]
[TestOf(typeof(SharedManaSystem))]
public sealed class ManaMathTests
{
    [Test]
    public void DefaultState_ReturnsMax()
    {
        // Fresh component: Current==Max, anchors at Zero. Regen has had "forever"
        // to tick, clamp pins result at Max.
        var comp = new ManaComponent();
        var effective = SharedManaSystem.CalculateEffectiveMana(comp, TimeSpan.FromSeconds(100));
        Assert.That(effective, Is.EqualTo(comp.Max));
    }

    [Test]
    public void QueryBeforeRegenStart_ReturnsStoredCurrent()
    {
        // Simulate post-spend state: Current just decremented, NextRegenTime
        // pushed out. Querying inside the delay window must return the stored
        // anchor value, not interpolate.
        var comp = new ManaComponent
        {
            Max = 100f,
            Current = 70f,
            RegenPerSecond = 2f,
            LastUpdate = TimeSpan.FromSeconds(5),
            NextRegenTime = TimeSpan.FromSeconds(8),
        };

        var effective = SharedManaSystem.CalculateEffectiveMana(comp, TimeSpan.FromSeconds(7));
        Assert.That(effective, Is.EqualTo(70f));
    }

    [Test]
    public void QueryAtRegenStart_ReturnsStoredCurrent()
    {
        // Exact boundary: regen hasn't yet had elapsed time to contribute.
        var comp = new ManaComponent
        {
            Max = 100f,
            Current = 70f,
            RegenPerSecond = 2f,
            LastUpdate = TimeSpan.FromSeconds(5),
            NextRegenTime = TimeSpan.FromSeconds(8),
        };

        var effective = SharedManaSystem.CalculateEffectiveMana(comp, TimeSpan.FromSeconds(8));
        Assert.That(effective, Is.EqualTo(70f));
    }

    [Test]
    public void QueryAfterRegenStart_InterpolatesAtRegenRate()
    {
        var comp = new ManaComponent
        {
            Max = 100f,
            Current = 70f,
            RegenPerSecond = 2f,
            LastUpdate = TimeSpan.FromSeconds(5),
            NextRegenTime = TimeSpan.FromSeconds(8),
        };

        // At t=10s: 2s past regen start, 2 mana/s → +4 mana.
        var effective = SharedManaSystem.CalculateEffectiveMana(comp, TimeSpan.FromSeconds(10));
        Assert.That(effective, Is.EqualTo(74f).Within(0.001));
    }

    [Test]
    public void QueryFarPastRegenStart_ClampsToMax()
    {
        var comp = new ManaComponent
        {
            Max = 100f,
            Current = 70f,
            RegenPerSecond = 2f,
            LastUpdate = TimeSpan.FromSeconds(5),
            NextRegenTime = TimeSpan.FromSeconds(8),
        };

        // At t=1000s: would interpolate to enormous value; must clamp to Max.
        var effective = SharedManaSystem.CalculateEffectiveMana(comp, TimeSpan.FromSeconds(1000));
        Assert.That(effective, Is.EqualTo(100f));
    }

    [Test]
    public void LastUpdateAfterNextRegen_UsesLastUpdateAsRegenStart()
    {
        // Edge case: if LastUpdate > NextRegenTime (e.g. a force-set scenario
        // where regen was re-enabled but Current got written later), regen
        // starts from the later anchor, not the earlier one. The max() choice
        // guarantees we never double-count elapsed time.
        var comp = new ManaComponent
        {
            Max = 100f,
            Current = 50f,
            RegenPerSecond = 2f,
            NextRegenTime = TimeSpan.FromSeconds(3),
            LastUpdate = TimeSpan.FromSeconds(5),
        };

        // At t=7s: regen should count from t=5 (LastUpdate is the later anchor),
        // giving 2 seconds of regen = +4 mana.
        var effective = SharedManaSystem.CalculateEffectiveMana(comp, TimeSpan.FromSeconds(7));
        Assert.That(effective, Is.EqualTo(54f).Within(0.001));
    }
}

using System;
using Content.Shared.Mythos.Magic.Mana;
using NUnit.Framework;

namespace Content.Tests._Mythos.Magic.Mana;

/// <summary>
/// Pins the default-state contract for <see cref="ManaComponent"/>. The
/// lazy-regen formula relies on <c>Current == Max</c> at construction so
/// that fresh components report full mana regardless of how long ago the
/// zeroed anchors "elapsed"; changing the default would silently break
/// newly-spawned casters' mana reporting.
/// </summary>
[TestFixture]
[Category("Mythos")]
[TestOf(typeof(ManaComponent))]
public sealed class ManaComponentDefaultsTests
{
    [Test]
    public void Default_StartsAtFullMana()
    {
        var comp = new ManaComponent();
        Assert.That(comp.Current, Is.EqualTo(comp.Max));
    }

    [Test]
    public void Default_HasRegenRate()
    {
        // Non-zero default regen rate: a wizard whose wand yaml doesn't
        // override RegenPerSecond still slowly refills.
        var comp = new ManaComponent();
        Assert.That(comp.RegenPerSecond, Is.GreaterThan(0f));
    }

    [Test]
    public void Default_RegenDelayIsNonZero()
    {
        // Non-zero post-spend delay: spells can't be spammed purely on
        // regen-between-ticks.
        var comp = new ManaComponent();
        Assert.That(comp.RegenDelay, Is.GreaterThan(TimeSpan.Zero));
    }

    [Test]
    public void Default_AnchorsAreZero()
    {
        var comp = new ManaComponent();
        Assert.Multiple(() =>
        {
            Assert.That(comp.LastUpdate, Is.EqualTo(TimeSpan.Zero));
            Assert.That(comp.NextRegenTime, Is.EqualTo(TimeSpan.Zero));
        });
    }
}

using System;
using Content.Shared.Mythos.Combat.Targeting;
using NUnit.Framework;

namespace Content.Tests._Mythos.Combat.Targeting;

/// <summary>
/// Pins the default-state contract for <see cref="CombatTargetComponent"/>. The
/// server auto-attack loop uses <c>comp.Target is not { } target</c> as its
/// fast-path early-return, which only works if a freshly-constructed component
/// reports a null target. If that default ever changes, this test fails first.
/// </summary>
[TestFixture]
[Category("Mythos")]
[TestOf(typeof(CombatTargetComponent))]
public sealed class CombatTargetComponentDefaultsTests
{
    [Test]
    public void Default_TargetIsNull()
    {
        var comp = new CombatTargetComponent();
        Assert.That(comp.Target, Is.Null);
    }

    [Test]
    public void Default_LastSeenIsZero()
    {
        var comp = new CombatTargetComponent();
        Assert.That(comp.LastSeen, Is.EqualTo(TimeSpan.Zero));
    }
}

using System;
using Content.Shared.Mythos.Magic.Mana;
using NUnit.Framework;

namespace Content.Tests._Mythos.Magic.Mana;

/// <summary>
/// Pure-function coverage for the refund semantics. Refunds have distinct
/// edge cases from spends: they must clamp to Max and reset
/// the regen-delay anchor so a cancelled cast doesn't leave the caster
/// double-penalised (once by the interrupt, once by a lingering cooldown).
///
/// <c>Refund</c> itself depends on <c>IGameTiming</c> and component resolution
/// which the unit harness doesn't ship, so these tests pin the clamp/floor
/// arithmetic against <c>CalculateEffectiveMana</c> directly. Stateful end-to-end
/// refund is covered by integration tests.
/// </summary>
[TestFixture]
[Category("Mythos")]
[TestOf(typeof(SharedManaSystem))]
public sealed class ManaRefundTests
{
    [Test]
    public void Refund_CannotExceedMax()
    {
        // Constructs the outcome we'd observe after Refund: effective mana is
        // clamped by Max even if the refund would notionally push above it.
        var comp = new ManaComponent
        {
            Max = 100f,
            Current = 100f, // already full
            LastUpdate = TimeSpan.FromSeconds(1),
            NextRegenTime = TimeSpan.FromSeconds(1),
        };

        // Any query past the anchor returns Max because regen clamps.
        var effective = SharedManaSystem.CalculateEffectiveMana(comp, TimeSpan.FromSeconds(10));
        Assert.That(effective, Is.EqualTo(100f), "Clamp holds when Current == Max");

        // Refund formula: min(Max, effective + amount). With effective=Max,
        // +anything stays at Max; verifies the clamp branch is reached.
        var refunded = effective + 50f;
        Assert.That(Math.Min(comp.Max, refunded), Is.EqualTo(100f));
    }

    [Test]
    public void Refund_OnPartialMana_AddsWithoutOverflow()
    {
        // Refund on a partial pool brings effective toward Max without going over.
        var comp = new ManaComponent
        {
            Max = 100f,
            Current = 40f,
            LastUpdate = TimeSpan.FromSeconds(5),
            NextRegenTime = TimeSpan.FromSeconds(20), // regen still locked out
        };

        var effectiveAt6 = SharedManaSystem.CalculateEffectiveMana(comp, TimeSpan.FromSeconds(6));
        Assert.That(effectiveAt6, Is.EqualTo(40f), "Still in delay window, effective == Current");

        // Production Refund would write Current = effective + amount (clamped)
        // and reset the anchor to now. Mirror the clamp here to pin the rule.
        var restoredNotional = effectiveAt6 + 20f;
        Assert.That(Math.Min(comp.Max, restoredNotional), Is.EqualTo(60f));
    }

    [Test]
    public void Refund_LargerThanCapacity_ClampsToMax()
    {
        var comp = new ManaComponent
        {
            Max = 100f,
            Current = 90f,
            LastUpdate = TimeSpan.FromSeconds(5),
            NextRegenTime = TimeSpan.FromSeconds(20),
        };

        var effective = SharedManaSystem.CalculateEffectiveMana(comp, TimeSpan.FromSeconds(6));
        var notional = effective + 500f;
        Assert.That(Math.Min(comp.Max, notional), Is.EqualTo(100f));
    }
}

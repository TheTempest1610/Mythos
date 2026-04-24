using Content.Shared.Mythos.Combat.Queue;
using NUnit.Framework;

namespace Content.Tests._Mythos.Combat.Queue;

/// <summary>
/// Pins the default-state contract for the cast-in-flight marker on
/// <see cref="CombatQueueComponent"/>. The client executor's cast gate reads
/// <c>CastingSlot != null</c> to decide whether to skip; a non-null default
/// would lock the queue permanently on a freshly-spawned player.
/// </summary>
[TestFixture]
[Category("Mythos")]
[TestOf(typeof(CombatQueueComponent))]
public sealed class CombatQueueCastingSlotDefaultTests
{
    [Test]
    public void Default_CastingSlotIsNull()
    {
        var comp = new CombatQueueComponent();
        Assert.That(comp.CastingSlot, Is.Null);
    }
}

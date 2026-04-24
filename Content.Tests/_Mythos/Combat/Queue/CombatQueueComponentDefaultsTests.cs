using Content.Shared.Mythos.Combat.Queue;
using NUnit.Framework;

namespace Content.Tests._Mythos.Combat.Queue;

/// <summary>
/// Pins the default-state contract for <see cref="CombatQueueComponent"/>. The
/// client-side <c>CombatQueueExecutor</c> short-circuits when
/// <c>queue.Queue.Count == 0</c>, so a freshly-constructed component must
/// start empty with a zero slot counter.
/// </summary>
[TestFixture]
[Category("Mythos")]
[TestOf(typeof(CombatQueueComponent))]
public sealed class CombatQueueComponentDefaultsTests
{
    [Test]
    public void Default_QueueIsEmpty()
    {
        var comp = new CombatQueueComponent();
        Assert.That(comp.Queue, Is.Empty);
    }

    [Test]
    public void Default_NextSlotIdIsZero()
    {
        var comp = new CombatQueueComponent();
        Assert.That(comp.NextSlotId, Is.EqualTo(0));
    }

    [Test]
    public void MaxSlots_IsFive()
    {
        // Queue depth is capped at 5. Downstream UX and balance assume this
        // number; if it changes, widgets and tests need revisiting.
        Assert.That(CombatQueueComponent.MaxSlots, Is.EqualTo(5));
    }
}

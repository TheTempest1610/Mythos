#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Mythos.Combat.Queue;
using Content.Server.Mythos.Magic.Mana;
using Content.Shared.Mythos.Combat.Queue;
using Content.Shared.Mythos.Magic.Mana;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.UnitTesting;

namespace Content.IntegrationTests._Mythos.Magic.MagicMissile;

/// <summary>
/// Integration coverage for Magic Missile's interaction with the
/// combat queue. End-to-end cast event handling is still best covered by
/// manual smoke testing because synthesizing a predictive networked cast
/// event from the test harness requires session fakery the fixture doesn't
/// cheaply support; the DoAfter integration introduced for cast-time spells
/// will offer a better harness to revisit that. These tests pin the queue
/// contract: MagicMissile
/// enqueues regardless of current mana (plan's "OOM rejects at dequeue, not
/// enqueue" invariant) and the head sits unconsumed when the executor's
/// preconditions (mana, target) aren't met.
/// </summary>
[TestFixture]
[Category("Mythos")]
public sealed class MagicMissileQueueTests : GameTest
{
    public override PoolSettings PoolSettings => new PoolSettings { Connected = true, DummyTicker = false };

    [Test]
    public async Task Enqueue_MagicMissile_SucceedsRegardlessOfMana()
    {
        var (server, sEntMan, player, queueSys, manaSys) = Resolve();
        await ResetPlayerState(server, sEntMan, player, manaSys);

        // Drain mana to zero so an enqueue-time check would reject.
        await server.WaitPost(() => manaSys.TrySpend(player, 100f));

        var enqueued = false;
        await server.WaitPost(() =>
            enqueued = queueSys.TryEnqueue(player, QueuedActionKind.MagicMissile, null));
        await RunTicks(2);

        var comp = sEntMan.GetComponent<CombatQueueComponent>(player);
        Assert.Multiple(() =>
        {
            Assert.That(enqueued, Is.True, "enqueue-on-OOM succeeds; the dequeue gate stalls later");
            Assert.That(comp.Queue.Count, Is.EqualTo(1));
            Assert.That(comp.Queue[0].Kind, Is.EqualTo(QueuedActionKind.MagicMissile));
        });
    }

    [Test]
    public async Task Enqueue_MixedKinds_PreservesOrder()
    {
        var (server, sEntMan, player, queueSys, manaSys) = Resolve();
        await ResetPlayerState(server, sEntMan, player, manaSys);

        await server.WaitPost(() =>
        {
            queueSys.TryEnqueue(player, QueuedActionKind.MagicMissile, null);
            queueSys.TryEnqueue(player, QueuedActionKind.HeavyAttack, null);
            queueSys.TryEnqueue(player, QueuedActionKind.MagicMissile, null);
        });
        await RunTicks(2);

        var comp = sEntMan.GetComponent<CombatQueueComponent>(player);
        Assert.That(comp.Queue.Select(q => q.Kind), Is.EqualTo(new[]
        {
            QueuedActionKind.MagicMissile,
            QueuedActionKind.HeavyAttack,
            QueuedActionKind.MagicMissile,
        }));
    }

    private (RobustIntegrationTest.ServerIntegrationInstance server,
             IEntityManager sEntMan,
             EntityUid player,
             CombatQueueSystem queueSys,
             ManaSystem manaSys) Resolve()
    {
        var server = Pair.Server;
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var queueSys = server.System<CombatQueueSystem>();
        var manaSys = server.System<ManaSystem>();
        var player = server.ResolveDependency<IPlayerManager>().Sessions.Single().AttachedEntity!.Value;
        return (server, sEntMan, player, queueSys, manaSys);
    }

    /// <summary>
    /// Restore player to a clean baseline: empty queue, fresh full mana
    /// pool. Pooled pairs may carry either component's prior state.
    /// </summary>
    private async Task ResetPlayerState(
        RobustIntegrationTest.ServerIntegrationInstance server,
        IEntityManager sEntMan,
        EntityUid player,
        ManaSystem _)
    {
        await server.WaitPost(() =>
        {
            if (sEntMan.HasComponent<CombatQueueComponent>(player))
                sEntMan.RemoveComponent<CombatQueueComponent>(player);
            if (sEntMan.HasComponent<ManaComponent>(player))
                sEntMan.RemoveComponent<ManaComponent>(player);
            sEntMan.AddComponent<ManaComponent>(player);
        });
        await RunTicks(1);
    }

    private async Task RunTicks(int count)
    {
        for (var i = 0; i < count; i++)
            await Pair.Server.WaitRunTicks(1);
    }
}

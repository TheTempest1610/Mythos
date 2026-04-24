#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Mythos.Combat.Queue;
using Content.Shared.Mythos.Combat.Queue;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.UnitTesting;

namespace Content.IntegrationTests._Mythos.Combat.Queue;

/// <summary>
/// Integration coverage for the combat queue's state-mutation API.
/// Each test spins up a paired server + client via
/// <c>PoolSettings { Connected = true, DummyTicker = false }</c> so the queue
/// component is mounted on a real player with combat-mode infrastructure.
///
/// These tests deliberately exercise the shared system's public API directly
/// (<c>TryEnqueue</c>, <c>CancelSlot</c>, <c>ClearQueue</c>, <c>PopHead</c>)
/// rather than simulating the full client-executor-fires-predictive-event
/// flow. That path is covered architecturally by the equivalent target-
/// selection pattern and would require mouse-input simulation that the test harness
/// doesn't cheaply support. The executor itself is thin glue; the risk
/// surface that tests protect is the shared mutation logic.
/// </summary>
[TestFixture]
[Category("Mythos")]
public sealed class CombatQueueTests : GameTest
{
    public override PoolSettings PoolSettings => new PoolSettings { Connected = true, DummyTicker = false };

    [Test]
    public async Task TryEnqueue_AppendsAndAssignsMonotonicSlots()
    {
        var (server, sEntMan, player, queueSys) = Resolve();
        await ClearQueueState(server, player);

        var s0 = false;
        var s1 = false;
        var s2 = false;
        await server.WaitPost(() =>
        {
            s0 = queueSys.TryEnqueue(player, QueuedActionKind.HeavyAttack, null);
            s1 = queueSys.TryEnqueue(player, QueuedActionKind.HeavyAttack, null);
            s2 = queueSys.TryEnqueue(player, QueuedActionKind.HeavyAttack, null);
        });
        await RunTicks(2);

        var comp = sEntMan.GetComponent<CombatQueueComponent>(player);
        Assert.Multiple(() =>
        {
            Assert.That(s0 && s1 && s2, Is.True);
            Assert.That(comp.Queue.Count, Is.EqualTo(3));
            Assert.That(comp.Queue[0].Slot, Is.EqualTo((ushort)0));
            Assert.That(comp.Queue[1].Slot, Is.EqualTo((ushort)1));
            Assert.That(comp.Queue[2].Slot, Is.EqualTo((ushort)2));
            Assert.That(comp.NextSlotId, Is.EqualTo((ushort)3));
        });
    }

    [Test]
    public async Task CancelSlot_RemovesOnlyThatSlot_PreservingNeighbors()
    {
        var (server, sEntMan, player, queueSys) = Resolve();
        await ClearQueueState(server, player);

        await server.WaitPost(() =>
        {
            queueSys.TryEnqueue(player, QueuedActionKind.HeavyAttack, null); // slot 0
            queueSys.TryEnqueue(player, QueuedActionKind.HeavyAttack, null); // slot 1
            queueSys.TryEnqueue(player, QueuedActionKind.HeavyAttack, null); // slot 2
        });
        await RunTicks(2);

        var cancelled = false;
        await server.WaitPost(() => cancelled = queueSys.CancelSlot(player, 1));
        await RunTicks(2);

        var comp = sEntMan.GetComponent<CombatQueueComponent>(player);
        Assert.Multiple(() =>
        {
            Assert.That(cancelled, Is.True);
            Assert.That(comp.Queue.Count, Is.EqualTo(2));
            Assert.That(comp.Queue.Select(q => q.Slot), Is.EqualTo(new ushort[] { 0, 2 }));
        });
    }

    [Test]
    public async Task CancelSlot_UnknownSlot_Rejected()
    {
        var (server, _, player, queueSys) = Resolve();
        await ClearQueueState(server, player);

        await server.WaitPost(() => queueSys.TryEnqueue(player, QueuedActionKind.HeavyAttack, null));
        await RunTicks(2);

        var cancelled = true;
        await server.WaitPost(() => cancelled = queueSys.CancelSlot(player, 99));

        Assert.That(cancelled, Is.False);
    }

    [Test]
    public async Task ClearQueue_EmptiesAllSlots_PreservesNextSlotId()
    {
        var (server, sEntMan, player, queueSys) = Resolve();
        await ClearQueueState(server, player);

        await server.WaitPost(() =>
        {
            queueSys.TryEnqueue(player, QueuedActionKind.HeavyAttack, null);
            queueSys.TryEnqueue(player, QueuedActionKind.HeavyAttack, null);
            queueSys.TryEnqueue(player, QueuedActionKind.HeavyAttack, null);
        });
        await RunTicks(2);
        await server.WaitPost(() => queueSys.ClearQueue(player));
        await RunTicks(2);

        var comp = sEntMan.GetComponent<CombatQueueComponent>(player);
        Assert.Multiple(() =>
        {
            Assert.That(comp.Queue, Is.Empty);
            // Slot IDs never recycle: NextSlotId remains at 3, not reset to 0.
            Assert.That(comp.NextSlotId, Is.EqualTo((ushort)3));
        });
    }

    [Test]
    public async Task TryEnqueue_BeyondMaxSlots_Rejected()
    {
        var (server, sEntMan, player, queueSys) = Resolve();
        await ClearQueueState(server, player);

        var results = new bool[CombatQueueComponent.MaxSlots + 1];
        await server.WaitPost(() =>
        {
            for (var i = 0; i < results.Length; i++)
                results[i] = queueSys.TryEnqueue(player, QueuedActionKind.HeavyAttack, null);
        });
        await RunTicks(2);

        var comp = sEntMan.GetComponent<CombatQueueComponent>(player);
        Assert.Multiple(() =>
        {
            for (var i = 0; i < CombatQueueComponent.MaxSlots; i++)
                Assert.That(results[i], Is.True, $"slot {i} should accept");
            Assert.That(results[^1], Is.False, "overflow enqueue should reject");
            Assert.That(comp.Queue.Count, Is.EqualTo(CombatQueueComponent.MaxSlots));
        });
    }

    [Test]
    public async Task PopHead_MatchingSlot_Advances()
    {
        var (server, sEntMan, player, queueSys) = Resolve();
        await ClearQueueState(server, player);

        await server.WaitPost(() =>
        {
            queueSys.TryEnqueue(player, QueuedActionKind.HeavyAttack, null); // slot 0
            queueSys.TryEnqueue(player, QueuedActionKind.HeavyAttack, null); // slot 1
        });
        await RunTicks(2);

        var popped = false;
        await server.WaitPost(() => popped = queueSys.PopHead(player, 0));
        await RunTicks(2);

        var comp = sEntMan.GetComponent<CombatQueueComponent>(player);
        Assert.Multiple(() =>
        {
            Assert.That(popped, Is.True);
            Assert.That(comp.Queue.Count, Is.EqualTo(1));
            Assert.That(comp.Queue[0].Slot, Is.EqualTo((ushort)1));
        });
    }

    [Test]
    public async Task PopHead_MismatchedSlot_Rejected()
    {
        var (server, sEntMan, player, queueSys) = Resolve();
        await ClearQueueState(server, player);

        await server.WaitPost(() => queueSys.TryEnqueue(player, QueuedActionKind.HeavyAttack, null)); // slot 0
        await RunTicks(2);

        var popped = true;
        await server.WaitPost(() => popped = queueSys.PopHead(player, 42));

        var comp = sEntMan.GetComponent<CombatQueueComponent>(player);
        Assert.Multiple(() =>
        {
            Assert.That(popped, Is.False);
            // Head still intact after mismatched pop.
            Assert.That(comp.Queue.Count, Is.EqualTo(1));
            Assert.That(comp.Queue[0].Slot, Is.EqualTo((ushort)0));
        });
    }

    [Test]
    public async Task PopHead_EmptyQueue_Rejected()
    {
        var (server, _, player, queueSys) = Resolve();
        await ClearQueueState(server, player);

        var popped = true;
        await server.WaitPost(() => popped = queueSys.PopHead(player, 0));

        Assert.That(popped, Is.False);
    }

    [Test]
    public async Task NetworkedQueue_SyncsToClient()
    {
        var pair = Pair;
        var server = pair.Server;
        var client = pair.Client;
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var cEntMan = client.ResolveDependency<IEntityManager>();
        var queueSys = server.System<CombatQueueSystem>();

        var player = server.ResolveDependency<IPlayerManager>().Sessions.Single().AttachedEntity!.Value;
        await ClearQueueState(server, player);

        await server.WaitPost(() =>
        {
            queueSys.TryEnqueue(player, QueuedActionKind.HeavyAttack, null);
            queueSys.TryEnqueue(player, QueuedActionKind.HeavyAttack, null);
        });

        // Tick enough for the server→client state to propagate.
        for (var i = 0; i < 8; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        var clientPlayer = client.Session!.AttachedEntity!.Value;
        var synced = cEntMan.TryGetComponent<CombatQueueComponent>(clientPlayer, out var clientComp);

        Assert.Multiple(() =>
        {
            Assert.That(synced, Is.True);
            Assert.That(clientComp!.Queue.Count, Is.EqualTo(2));
        });
    }

    private (RobustIntegrationTest.ServerIntegrationInstance server,
             IEntityManager sEntMan,
             EntityUid player,
             CombatQueueSystem queueSys) Resolve()
    {
        var server = Pair.Server;
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var queueSys = server.System<CombatQueueSystem>();
        var player = server.ResolveDependency<IPlayerManager>().Sessions.Single().AttachedEntity!.Value;
        return (server, sEntMan, player, queueSys);
    }

    /// <summary>
    /// Pooled pairs may carry a <see cref="CombatQueueComponent"/> from a prior
    /// test. Remove it so the next <c>EnsureComp</c> inside <c>TryEnqueue</c>
    /// mints a fresh component with an empty queue and a zeroed slot counter,
    /// making assertions order-independent.
    /// </summary>
    private async Task ClearQueueState(
        RobustIntegrationTest.ServerIntegrationInstance server,
        EntityUid player)
    {
        var sEntMan = server.ResolveDependency<IEntityManager>();
        await server.WaitPost(() =>
        {
            if (sEntMan.HasComponent<CombatQueueComponent>(player))
                sEntMan.RemoveComponent<CombatQueueComponent>(player);
        });
        await RunTicks(1);
    }

    private async Task RunTicks(int count)
    {
        for (var i = 0; i < count; i++)
            await Pair.Server.WaitRunTicks(1);
    }
}

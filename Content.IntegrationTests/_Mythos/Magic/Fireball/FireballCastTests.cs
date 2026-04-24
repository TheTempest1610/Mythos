#nullable enable
using System;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Mythos.Combat.Queue;
using Content.Server.Mythos.Magic.Fireball;
using Content.Server.Mythos.Magic.Mana;
using Content.Shared.DoAfter;
using Content.Shared.Mythos.Combat.Queue;
using Content.Shared.Mythos.Magic.Fireball;
using Content.Shared.Mythos.Magic.Mana;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.UnitTesting;

namespace Content.IntegrationTests._Mythos.Magic.Fireball;

/// <summary>
/// Integration coverage for the Fireball cast lifecycle. Exercises
/// the full server-authoritative flow: enqueue Fireball → start cast (spend
/// mana, start DoAfter, flag <see cref="CombatQueueComponent.CastingSlot"/>) →
/// either completion (pop queue head, clear flag) or cancellation (refund
/// mana, clear flag, leave queue head intact for retry).
/// </summary>
[TestFixture]
[Category("Mythos")]
public sealed class FireballCastTests : GameTest
{
    public override PoolSettings PoolSettings => new PoolSettings { Connected = true, DummyTicker = false };

    [Test]
    public async Task TryStartFireball_Valid_SpendsManaAndFlagsCasting()
    {
        var ctx = await StartFreshWithQueuedFireball();
        var slot = ctx.HeadSlot;

        var started = false;
        await ctx.Server.WaitPost(() =>
            started = ctx.FireballSys.TryStartFireball(ctx.Player, null, ctx.Location, slot));
        await RunTicks(2);

        var queue = ctx.SEntMan.GetComponent<CombatQueueComponent>(ctx.Player);
        Assert.Multiple(() =>
        {
            Assert.That(started, Is.True);
            Assert.That(queue.CastingSlot, Is.EqualTo((ushort?)slot));
            Assert.That(ctx.ManaSys.GetEffectiveMana(ctx.Player),
                Is.EqualTo(100f - SharedFireballSystem.ManaCost).Within(0.5f));
            // Queue entry remains until the cast finishes.
            Assert.That(queue.Queue.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task TryStartFireball_DuplicateWhileCasting_Rejected()
    {
        var ctx = await StartFreshWithQueuedFireball();
        var slot = ctx.HeadSlot;

        // First start succeeds.
        await ctx.Server.WaitPost(() => ctx.FireballSys.TryStartFireball(ctx.Player, null, ctx.Location, slot));
        await RunTicks(1);
        await RunTicks(2);

        // Second start while the first is in flight must reject without
        // double-spending mana or orphaning the existing DoAfter.
        var manaBefore = 0f;
        var secondStarted = true;
        var manaAfter = 0f;
        await ctx.Server.WaitPost(() =>
        {
            manaBefore = ctx.ManaSys.GetEffectiveMana(ctx.Player);
            secondStarted = ctx.FireballSys.TryStartFireball(ctx.Player, null, ctx.Location, slot);
            manaAfter = ctx.ManaSys.GetEffectiveMana(ctx.Player);
        });

        Assert.Multiple(() =>
        {
            Assert.That(secondStarted, Is.False);
            Assert.That(manaAfter, Is.EqualTo(manaBefore).Within(0.01), "No double spend");
        });
    }

    [Test]
    public async Task TryStartFireball_InsufficientMana_Rejected()
    {
        var ctx = await StartFreshWithQueuedFireball();
        var slot = ctx.HeadSlot;

        // Drain mana below the Fireball cost.
        await ctx.Server.WaitPost(() => ctx.ManaSys.TrySpend(ctx.Player, 90f));

        var started = true;
        await ctx.Server.WaitPost(() =>
            started = ctx.FireballSys.TryStartFireball(ctx.Player, null, ctx.Location, slot));

        var queue = ctx.SEntMan.GetComponent<CombatQueueComponent>(ctx.Player);
        Assert.Multiple(() =>
        {
            Assert.That(started, Is.False);
            Assert.That(queue.CastingSlot, Is.Null);
            // Queue entry untouched; player can retry once mana regenerates.
            Assert.That(queue.Queue.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task TryStartFireball_QueueHeadNotFireball_Rejected()
    {
        var ctx = await StartFreshWithQueuedFireball();
        var slot = ctx.HeadSlot;

        // Replace the Fireball head with a HeavyAttack so the kind check fails.
        await ctx.Server.WaitPost(() =>
        {
            ctx.QueueSys.ClearQueue(ctx.Player);
            ctx.QueueSys.TryEnqueue(ctx.Player, QueuedActionKind.HeavyAttack, null);
        });
        await RunTicks(1);

        var started = true;
        await ctx.Server.WaitPost(() =>
            started = ctx.FireballSys.TryStartFireball(ctx.Player, null, ctx.Location, slot));
        Assert.That(started, Is.False);
    }

    [Test]
    public async Task Cast_Completes_PopsQueueAndClearsFlag()
    {
        var ctx = await StartFreshWithQueuedFireball();
        var slot = ctx.HeadSlot;

        await ctx.Server.WaitPost(() => ctx.FireballSys.TryStartFireball(ctx.Player, null, ctx.Location, slot));
        await RunTicks(1);

        // Advance past the 1.5s cast time plus DoAfter tick slop.
        // Default server tick is 60Hz, so 120 ticks ≈ 2 seconds.
        await RunTicks(120);

        var queue = ctx.SEntMan.GetComponent<CombatQueueComponent>(ctx.Player);
        Assert.Multiple(() =>
        {
            Assert.That(queue.CastingSlot, Is.Null, "Cast-flight flag cleared on completion");
            Assert.That(queue.Queue.Any(q => q.Slot == slot), Is.False, "Completed slot removed");
        });
    }

    [Test]
    public async Task Cast_CancelledMidFlight_RefundsManaAndLeavesQueueIntact()
    {
        var ctx = await StartFreshWithQueuedFireball();
        var slot = ctx.HeadSlot;

        await ctx.Server.WaitPost(() => ctx.FireballSys.TryStartFireball(ctx.Player, null, ctx.Location, slot));
        await RunTicks(1);
        await RunTicks(2);

        var manaMidCast = 0f;
        await ctx.Server.WaitPost(() => manaMidCast = ctx.ManaSys.GetEffectiveMana(ctx.Player));

        // Cancel the in-flight DoAfter directly; exercises the same cancellation
        // path that BreakOnDamage / BreakOnMove take inside SharedDoAfterSystem.
        await ctx.Server.WaitPost(() =>
        {
            if (!ctx.SEntMan.TryGetComponent<DoAfterComponent>(ctx.Player, out var doAfterComp))
                return;
            foreach (var da in doAfterComp.DoAfters.Values)
            {
                if (da.Args.Event is FireballDoAfterEvent)
                {
                    ctx.DoAfterSys.Cancel(da.Id);
                    break;
                }
            }
        });
        await RunTicks(2);

        var queue = ctx.SEntMan.GetComponent<CombatQueueComponent>(ctx.Player);
        var manaAfterCancel = 0f;
        await ctx.Server.WaitPost(() => manaAfterCancel = ctx.ManaSys.GetEffectiveMana(ctx.Player));

        Assert.Multiple(() =>
        {
            Assert.That(queue.CastingSlot, Is.Null, "Cast-flight flag cleared on cancel");
            Assert.That(queue.Queue.Any(q => q.Slot == slot), Is.True, "Cancelled slot stays for retry");
            Assert.That(manaAfterCancel, Is.GreaterThan(manaMidCast), "Mana refunded after cancel");
        });
    }

    // ---- Test context plumbing ----

    private sealed class Ctx
    {
        public RobustIntegrationTest.ServerIntegrationInstance Server = default!;
        public IEntityManager SEntMan = default!;
        public EntityUid Player;
        public FireballSystem FireballSys = default!;
        public ManaSystem ManaSys = default!;
        public CombatQueueSystem QueueSys = default!;
        public SharedDoAfterSystem DoAfterSys = default!;
        public NetCoordinates Location;
        public ushort HeadSlot;
    }

    private async Task<Ctx> StartFreshWithQueuedFireball()
    {
        var server = Pair.Server;
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var player = server.ResolveDependency<IPlayerManager>().Sessions.Single().AttachedEntity!.Value;
        var ctx = new Ctx
        {
            Server = server,
            SEntMan = sEntMan,
            Player = player,
            FireballSys = server.System<FireballSystem>(),
            ManaSys = server.System<ManaSystem>(),
            QueueSys = server.System<CombatQueueSystem>(),
            DoAfterSys = server.System<SharedDoAfterSystem>(),
        };

        // Reset to a known baseline: fresh mana, empty queue with one Fireball
        // at head, no in-flight cast.
        var headSlot = (ushort)0;
        var location = default(NetCoordinates);
        await server.WaitPost(() =>
        {
            if (sEntMan.HasComponent<CombatQueueComponent>(player))
                sEntMan.RemoveComponent<CombatQueueComponent>(player);
            if (sEntMan.HasComponent<ManaComponent>(player))
                sEntMan.RemoveComponent<ManaComponent>(player);
            sEntMan.AddComponent<ManaComponent>(player);
            ctx.QueueSys.TryEnqueue(player, QueuedActionKind.Fireball, null);
            var q = sEntMan.GetComponent<CombatQueueComponent>(player);
            headSlot = q.Queue[0].Slot;
            var playerCoords = sEntMan.GetComponent<TransformComponent>(player).Coordinates;
            location = sEntMan.GetNetCoordinates(playerCoords);
        });
        await RunTicks(1);
        ctx.HeadSlot = headSlot;
        ctx.Location = location;
        return ctx;
    }

    private async Task RunTicks(int count)
    {
        for (var i = 0; i < count; i++)
            await Pair.Server.WaitRunTicks(1);
    }
}

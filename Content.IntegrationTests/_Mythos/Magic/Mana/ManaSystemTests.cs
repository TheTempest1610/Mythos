#nullable enable
using System;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Mythos.Magic.Mana;
using Content.Shared.Mythos.Magic.Mana;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.UnitTesting;

namespace Content.IntegrationTests._Mythos.Magic.Mana;

/// <summary>
/// Integration coverage for <see cref="SharedManaSystem"/>. Exercises
/// the stateful <c>TrySpend</c> path end-to-end against a real player with
/// game-time advancing across ticks. Pure-function regen math is pinned
/// separately by the unit-test fixture.
/// </summary>
[TestFixture]
[Category("Mythos")]
public sealed class ManaSystemTests : GameTest
{
    public override PoolSettings PoolSettings => new PoolSettings { Connected = true, DummyTicker = false };

    [Test]
    public async Task TrySpend_Sufficient_DecrementsAndSetsAnchors()
    {
        var (server, sEntMan, player, manaSys) = Resolve();
        await InstallFreshMana(server, sEntMan, player);

        var before = 0f;
        var spent = false;
        var after = 0f;
        await server.WaitPost(() =>
        {
            before = manaSys.GetEffectiveMana(player);
            spent = manaSys.TrySpend(player, 30f);
            after = manaSys.GetEffectiveMana(player);
        });

        var comp = sEntMan.GetComponent<ManaComponent>(player);
        Assert.Multiple(() =>
        {
            Assert.That(spent, Is.True);
            Assert.That(before, Is.EqualTo(100f).Within(0.01));
            Assert.That(after, Is.EqualTo(70f).Within(0.01));
            // Anchors advanced; future regen will count from post-spend time.
            Assert.That(comp.LastUpdate, Is.GreaterThan(TimeSpan.Zero));
            Assert.That(comp.NextRegenTime, Is.GreaterThan(comp.LastUpdate));
        });
    }

    [Test]
    public async Task TrySpend_Insufficient_RejectsWithoutMutation()
    {
        var (server, sEntMan, player, manaSys) = Resolve();
        await InstallFreshMana(server, sEntMan, player);

        // Drain to 20 mana first.
        await server.WaitPost(() => manaSys.TrySpend(player, 80f));

        var currentBefore = 0f;
        var lastUpdateBefore = TimeSpan.Zero;
        var spent = true;
        var currentAfter = 0f;
        var lastUpdateAfter = TimeSpan.Zero;
        await server.WaitPost(() =>
        {
            var comp = sEntMan.GetComponent<ManaComponent>(player);
            currentBefore = comp.Current;
            lastUpdateBefore = comp.LastUpdate;
            spent = manaSys.TrySpend(player, 50f);
            currentAfter = comp.Current;
            lastUpdateAfter = comp.LastUpdate;
        });

        Assert.Multiple(() =>
        {
            Assert.That(spent, Is.False);
            Assert.That(currentAfter, Is.EqualTo(currentBefore), "Current unchanged on rejected spend");
            Assert.That(lastUpdateAfter, Is.EqualTo(lastUpdateBefore), "Anchor unchanged on rejected spend");
        });
    }

    [Test]
    public async Task TrySpend_ZeroAmount_IsNoOpReturningTrue()
    {
        var (server, sEntMan, player, manaSys) = Resolve();
        await InstallFreshMana(server, sEntMan, player);

        var currentBefore = 0f;
        var spent = false;
        var currentAfter = 0f;
        await server.WaitPost(() =>
        {
            var comp = sEntMan.GetComponent<ManaComponent>(player);
            currentBefore = comp.Current;
            spent = manaSys.TrySpend(player, 0f);
            currentAfter = comp.Current;
        });

        Assert.Multiple(() =>
        {
            Assert.That(spent, Is.True, "Zero cost succeeds");
            Assert.That(currentAfter, Is.EqualTo(currentBefore), "Zero cost does not mutate");
        });
    }

    [Test]
    public async Task TrySpend_Negative_Rejected()
    {
        var (server, _, player, manaSys) = Resolve();
        await InstallFreshMana(server, Pair.Server.ResolveDependency<IEntityManager>(), player);

        var spent = true;
        await server.WaitPost(() => spent = manaSys.TrySpend(player, -5f));

        Assert.That(spent, Is.False, "Negative cost is rejected");
    }

    [Test]
    public async Task TrySpend_NoComponent_Rejected()
    {
        var (server, sEntMan, player, manaSys) = Resolve();
        await server.WaitPost(() =>
        {
            if (sEntMan.HasComponent<ManaComponent>(player))
                sEntMan.RemoveComponent<ManaComponent>(player);
        });
        await RunTicks(1);

        var spent = true;
        await server.WaitPost(() => spent = manaSys.TrySpend(player, 1f));

        Assert.That(spent, Is.False, "Spending without a ManaComponent fails cleanly");
    }

    [Test]
    public async Task RegenDelay_SuppressesRegenInWindow()
    {
        var (server, _, player, manaSys) = Resolve();
        var sEntMan = Pair.Server.ResolveDependency<IEntityManager>();
        await InstallFreshMana(server, sEntMan, player);

        await server.WaitPost(() => manaSys.TrySpend(player, 50f));

        // Immediately after spend: effective mana must equal stored Current
        // because NextRegenTime is in the future.
        var immediately = 0f;
        await server.WaitPost(() => immediately = manaSys.GetEffectiveMana(player));
        Assert.That(immediately, Is.EqualTo(50f).Within(0.1), "Regen suppressed inside delay window");
    }

    [Test]
    public async Task Regen_ResumesAfterDelayElapses()
    {
        var (server, _, player, manaSys) = Resolve();
        var sEntMan = Pair.Server.ResolveDependency<IEntityManager>();
        await InstallFreshMana(server, sEntMan, player);

        // Shorten the regen delay so the test advances reasonable tick counts.
        await server.WaitPost(() =>
        {
            var comp = sEntMan.GetComponent<ManaComponent>(player);
            comp.RegenDelay = TimeSpan.FromMilliseconds(200);
            comp.RegenPerSecond = 10f;
        });

        await server.WaitPost(() => manaSys.TrySpend(player, 50f));

        // Advance past the regen delay and accumulate a few ticks of regen.
        // Default server tick is 60Hz (~16ms/tick), so 120 ticks ≈ 2 seconds.
        await RunTicks(120);

        var effective = 0f;
        await server.WaitPost(() => effective = manaSys.GetEffectiveMana(player));
        Assert.That(effective, Is.GreaterThan(50f), "Regen resumed after delay");
    }

    private (RobustIntegrationTest.ServerIntegrationInstance server,
             IEntityManager sEntMan,
             EntityUid player,
             ManaSystem manaSys) Resolve()
    {
        var server = Pair.Server;
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var manaSys = server.System<ManaSystem>();
        var player = server.ResolveDependency<IPlayerManager>().Sessions.Single().AttachedEntity!.Value;
        return (server, sEntMan, player, manaSys);
    }

    /// <summary>
    /// Pooled pairs may carry a ManaComponent from a prior test; remove it
    /// then re-add with defaults so subsequent assertions observe a known state.
    /// </summary>
    private async Task InstallFreshMana(
        RobustIntegrationTest.ServerIntegrationInstance server,
        IEntityManager sEntMan,
        EntityUid player)
    {
        await server.WaitPost(() =>
        {
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

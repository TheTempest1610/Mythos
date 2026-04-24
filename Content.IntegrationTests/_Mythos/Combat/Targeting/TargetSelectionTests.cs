#nullable enable
using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server.Mythos.Combat.Targeting;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mythos.Combat.Targeting;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.UnitTesting;

namespace Content.IntegrationTests._Mythos.Combat.Targeting;

/// <summary>
/// Integration coverage for target selection, target validation, and the
/// server-side auto-attack loop. Each test spins up a paired server + client via
/// <c>PoolSettings { Connected = true, DummyTicker = false }</c> so the spawned
/// player is a real, controllable mob with combat-mode and melee infrastructure.
/// </summary>
[TestFixture]
[Category("Mythos")]
public sealed class TargetSelectionTests : GameTest
{
    public override PoolSettings PoolSettings => new PoolSettings { Connected = true, DummyTicker = false };

    private const string DummyMobProto = "MythosCombatDummyMob";
    private const string DummyDamageableProto = "MythosCombatDummyDamageable";

    // Minimal prototypes for Mythos combat integration tests:
    //   MythosCombatDummyMob: just MobState; tests the mob branch of IsValidTarget.
    //   MythosCombatDummyDamageable: just Damageable; tests the non-mob
    //     damageable branch (walls / doors / destructibles).
    [TestPrototypes]
    private const string ExtraPrototypes = @"
- type: entity
  id: MythosCombatDummyMob
  components:
  - type: MobState
  - type: Transform

- type: entity
  id: MythosCombatDummyDamageable
  components:
  - type: Damageable
    damageContainer: Inorganic
  - type: Transform
";

    [Test]
    public async Task ValidMobTarget_SetTarget_PopulatesComponent()
    {
        var (server, sEntMan, player, targetSys) = Resolve();
        var target = await SpawnDummyMob(server, sEntMan, player);

        var accepted = false;
        await server.WaitPost(() => accepted = targetSys.TrySetTarget(player, target));
        await RunTicks(4);

        var comp = sEntMan.GetComponent<CombatTargetComponent>(player);
        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.True);
            Assert.That(comp.Target.HasValue, Is.True);
            Assert.That(comp.Target!.Value, Is.EqualTo(target));
        });
    }

    [Test]
    public async Task SelfTarget_IsRejected()
    {
        var (server, sEntMan, player, targetSys) = Resolve();
        await ClearTargetState(server, sEntMan, player);

        var accepted = true;
        await server.WaitPost(() => accepted = targetSys.TrySetTarget(player, player));

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.False);
            AssertNoTargetSet(sEntMan, player);
        });
    }

    [Test]
    public async Task NonMobNonDamageableTarget_IsRejected()
    {
        // A bare entity has neither MobStateComponent nor DamageableComponent
        // and must be rejected; targeting empty tiles / fluff entities would
        // let the auto-attack loop lock onto nothing.
        var (server, sEntMan, player, targetSys) = Resolve();
        await ClearTargetState(server, sEntMan, player);

        var nonMob = EntityUid.Invalid;
        await server.WaitPost(() =>
        {
            var coords = sEntMan.GetComponent<TransformComponent>(player).Coordinates;
            nonMob = sEntMan.SpawnAtPosition(null, coords);
        });
        await RunTicks(2);

        var accepted = true;
        await server.WaitPost(() => accepted = targetSys.TrySetTarget(player, nonMob));

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.False);
            AssertNoTargetSet(sEntMan, player);
        });
    }

    [Test]
    public async Task DamageableNonMobTarget_IsAccepted()
    {
        // Structures / walls / doors: anything with DamageableComponent but
        // no MobStateComponent should be targetable. This is the regression
        // case: before the fix, humans with both components worked but
        // structures with only Damageable got rejected.
        var (server, sEntMan, player, targetSys) = Resolve();
        await ClearTargetState(server, sEntMan, player);

        var damageable = EntityUid.Invalid;
        await server.WaitPost(() =>
        {
            var coords = sEntMan.GetComponent<TransformComponent>(player).Coordinates;
            damageable = sEntMan.SpawnAtPosition(DummyDamageableProto, coords);
        });
        await RunTicks(2);

        // Sanity: confirm the prototype did attach DamageableComponent.
        // A missing component here means the YAML didn't load the class,
        // not that IsValidTarget is broken.
        Assert.That(sEntMan.HasComponent<Content.Shared.Damage.Components.DamageableComponent>(damageable),
            Is.True, "Test prototype MythosCombatDummyDamageable must provide DamageableComponent");

        var accepted = false;
        await server.WaitPost(() => accepted = targetSys.TrySetTarget(player, damageable));

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.True);
            Assert.That(sEntMan.GetComponent<CombatTargetComponent>(player).Target,
                Is.EqualTo((EntityUid?)damageable));
        });
    }

    [Test]
    public async Task DeadMobTarget_IsRejected()
    {
        var (server, sEntMan, player, targetSys) = Resolve();
        await ClearTargetState(server, sEntMan, player);
        var target = await SpawnDummyMob(server, sEntMan, player);

        var mobStateSys = server.System<MobStateSystem>();
        await server.WaitPost(() => mobStateSys.ChangeMobState(target, MobState.Dead));
        await RunTicks(2);

        var accepted = true;
        await server.WaitPost(() => accepted = targetSys.TrySetTarget(player, target));

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.False);
            AssertNoTargetSet(sEntMan, player);
        });
    }

    [Test]
    public async Task ClearTarget_ResetsTargetToNull()
    {
        var (server, sEntMan, player, targetSys) = Resolve();
        var target = await SpawnDummyMob(server, sEntMan, player);

        await server.WaitPost(() => targetSys.TrySetTarget(player, target));
        await RunTicks(2);
        await server.WaitPost(() => targetSys.ClearTarget(player));
        await RunTicks(2);

        var comp = sEntMan.GetComponent<CombatTargetComponent>(player);
        Assert.That(comp.Target, Is.Null);
    }

    [Test]
    public async Task TargetDies_AutoAttackLoop_ClearsComponent()
    {
        var (server, sEntMan, player, targetSys) = Resolve();
        var target = await SpawnDummyMob(server, sEntMan, player);

        await server.WaitPost(() => targetSys.TrySetTarget(player, target));
        await RunTicks(2);

        var mobStateSys = server.System<MobStateSystem>();
        await server.WaitPost(() => mobStateSys.ChangeMobState(target, MobState.Dead));
        await RunTicks(4);

        var comp = sEntMan.GetComponent<CombatTargetComponent>(player);
        Assert.That(comp.Target, Is.Null);
    }

    [Test]
    public async Task TargetDeleted_AutoAttackLoop_ClearsComponent()
    {
        var (server, sEntMan, player, targetSys) = Resolve();
        var target = await SpawnDummyMob(server, sEntMan, player);

        await server.WaitPost(() => targetSys.TrySetTarget(player, target));
        await RunTicks(2);

        await server.WaitPost(() => sEntMan.DeleteEntity(target));
        await RunTicks(4);

        var comp = sEntMan.GetComponent<CombatTargetComponent>(player);
        Assert.That(comp.Target, Is.Null);
    }

    [Test]
    public async Task LongRangeTarget_IsAccepted()
    {
        // Regression for the removed melee-range gate: a mob placed well outside
        // any weapon's range must still be a valid target selection. The server
        // side never range-gated; this test pins the contract so a future refactor
        // that reintroduces range-on-select will fail here.
        var (server, sEntMan, player, targetSys) = Resolve();

        var target = EntityUid.Invalid;
        await server.WaitPost(() =>
        {
            var playerXform = sEntMan.GetComponent<TransformComponent>(player);
            var farCoords = playerXform.Coordinates.Offset(new Vector2(50f, 50f));
            target = sEntMan.SpawnAtPosition(DummyMobProto, farCoords);
        });
        await RunTicks(2);

        var accepted = false;
        await server.WaitPost(() => accepted = targetSys.TrySetTarget(player, target));
        await RunTicks(2);

        var comp = sEntMan.GetComponent<CombatTargetComponent>(player);
        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.True);
            Assert.That(comp.Target.HasValue, Is.True);
            Assert.That(comp.Target!.Value, Is.EqualTo(target));
        });
    }

    [Test]
    public async Task NetworkedComponent_SyncsToClient()
    {
        var pair = Pair;
        var server = pair.Server;
        var client = pair.Client;
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var cEntMan = client.ResolveDependency<IEntityManager>();
        var targetSys = server.System<CombatTargetSystem>();

        var player = server.ResolveDependency<IPlayerManager>().Sessions.Single().AttachedEntity!.Value;
        var target = await SpawnDummyMob(server, sEntMan, player);

        await server.WaitPost(() => targetSys.TrySetTarget(player, target));

        // Tick enough for the server→client state to propagate.
        for (var i = 0; i < 8; i++)
        {
            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);
        }

        var clientPlayer = client.Session!.AttachedEntity!.Value;
        var synced = cEntMan.TryGetComponent<CombatTargetComponent>(clientPlayer, out var clientComp);

        Assert.Multiple(() =>
        {
            Assert.That(synced, Is.True);
            Assert.That(clientComp!.Target.HasValue, Is.True);
        });
    }

    /// <summary>Common handles used by most tests in this fixture.</summary>
    private (RobustIntegrationTest.ServerIntegrationInstance server,
             IEntityManager sEntMan,
             EntityUid player,
             CombatTargetSystem targetSys) Resolve()
    {
        var server = Pair.Server;
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var targetSys = server.System<CombatTargetSystem>();
        var player = server.ResolveDependency<IPlayerManager>().Sessions.Single().AttachedEntity!.Value;
        return (server, sEntMan, player, targetSys);
    }

    private async Task<EntityUid> SpawnDummyMob(
        RobustIntegrationTest.ServerIntegrationInstance server,
        IEntityManager sEntMan,
        EntityUid player)
    {
        var target = EntityUid.Invalid;
        await server.WaitPost(() =>
        {
            var coords = sEntMan.GetComponent<TransformComponent>(player).Coordinates;
            target = sEntMan.SpawnAtPosition(DummyMobProto, coords);
        });
        await RunTicks(2);
        return target;
    }

    private async Task RunTicks(int count)
    {
        for (var i = 0; i < count; i++)
            await Pair.Server.WaitRunTicks(1);
    }

    /// <summary>
    /// Ensures the player starts the test with no active combat target. Necessary
    /// because <see cref="PoolManager"/> reuses server+client pairs across tests,
    /// so <see cref="CombatTargetComponent"/> added by an earlier test can persist.
    /// </summary>
    private async Task ClearTargetState(
        RobustIntegrationTest.ServerIntegrationInstance server,
        IEntityManager sEntMan,
        EntityUid player)
    {
        await server.WaitPost(() =>
        {
            if (sEntMan.HasComponent<CombatTargetComponent>(player))
                sEntMan.RemoveComponent<CombatTargetComponent>(player);
        });
        await RunTicks(1);
    }

    /// <summary>
    /// Asserts the player has no active combat target. Tolerates the component
    /// being present with a null <c>Target</c>, since pooled pairs may carry a
    /// residual component from prior tests.
    /// </summary>
    private static void AssertNoTargetSet(IEntityManager sEntMan, EntityUid player)
    {
        if (sEntMan.TryGetComponent<CombatTargetComponent>(player, out var comp))
            Assert.That(comp.Target, Is.Null);
    }
}

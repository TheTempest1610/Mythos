#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Mythos.Combat.Queue;
using Content.Server.Mythos.Player;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Mythos.Combat.Queue;
using Content.Shared.Mythos.Magic.Actions;
using Content.Shared.Mythos.Magic.Mana;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.IntegrationTests._Mythos.Player;

/// <summary>
/// Verifies the spell loadout wiring: on <c>PlayerAttachedEvent</c>,
/// the player mob receives a <see cref="ManaComponent"/> plus the two
/// Mythos spell action entities (Fireball, Magic Missile), and activating
/// a spell action enqueues the corresponding kind into the combat queue.
///
/// The loadout runs as part of the test pair's standard spawn sequence, so
/// these assertions simply observe post-setup state; no explicit call to
/// the loadout system is needed.
/// </summary>
[TestFixture]
[Category("Mythos")]
public sealed class MythosSpellLoadoutTests : GameTest
{
    public override PoolSettings PoolSettings => new PoolSettings { Connected = true, DummyTicker = false };

    [Test]
    public async Task PlayerAttach_GrantsManaAndLoadoutMarker()
    {
        var (_, sEntMan, player) = Resolve();

        Assert.Multiple(() =>
        {
            Assert.That(sEntMan.HasComponent<ManaComponent>(player),
                Is.True, "ManaComponent granted on attach");
            Assert.That(sEntMan.HasComponent<MythosSpellLoadoutComponent>(player),
                Is.True, "Loadout marker added to prevent duplicate grants");
        });
    }

    [Test]
    public async Task PlayerAttach_GrantsFireballAndMagicMissileActions()
    {
        var server = Pair.Server;
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var actionsSys = server.System<SharedActionsSystem>();
        var player = server.ResolveDependency<IPlayerManager>().Sessions.Single().AttachedEntity!.Value;

        Assert.That(sEntMan.HasComponent<ActionsComponent>(player),
            Is.True, "Player has ActionsComponent by default");

        var actions = actionsSys.GetActions(player).ToArray();
        var protoMan = server.ResolveDependency<Robust.Shared.Prototypes.IPrototypeManager>();

        // Each granted action is its own spawned entity whose metaName matches
        // the prototype's name field; look up the prototype on each action
        // entity and check we have the two Mythos spells.
        var haveFireball = false;
        var haveMagicMissile = false;

        foreach (var (actionEnt, _) in actions)
        {
            var meta = sEntMan.GetComponent<MetaDataComponent>(actionEnt);
            if (meta.EntityPrototype == null)
                continue;

            if (meta.EntityPrototype.ID == "ActionMythosFireball")
                haveFireball = true;
            else if (meta.EntityPrototype.ID == "ActionMythosMagicMissile")
                haveMagicMissile = true;
        }

        Assert.Multiple(() =>
        {
            Assert.That(haveFireball, Is.True, "Fireball action granted");
            Assert.That(haveMagicMissile, Is.True, "Magic Missile action granted");
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task MagicMissileAction_Enqueues()
    {
        var (server, sEntMan, player) = Resolve();

        // Clear any prior queue state so the assertion is order-independent.
        var queueSys = server.System<CombatQueueSystem>();
        await server.WaitPost(() =>
        {
            if (sEntMan.HasComponent<CombatQueueComponent>(player))
                sEntMan.RemoveComponent<CombatQueueComponent>(player);
        });
        await RunTicks(1);

        await server.WaitPost(() =>
        {
            var ev = new QueueMagicMissileActionEvent { Performer = player };
            sEntMan.EventBus.RaiseLocalEvent(player, ev, true);
        });
        await RunTicks(2);

        Assert.That(sEntMan.HasComponent<CombatQueueComponent>(player), Is.True);
        var queue = sEntMan.GetComponent<CombatQueueComponent>(player);
        Assert.Multiple(() =>
        {
            Assert.That(queue.Queue.Count, Is.EqualTo(1));
            Assert.That(queue.Queue[0].Kind, Is.EqualTo(QueuedActionKind.MagicMissile));
        });
    }

    [Test]
    public async Task FireballAction_Enqueues()
    {
        var (server, sEntMan, player) = Resolve();

        await server.WaitPost(() =>
        {
            if (sEntMan.HasComponent<CombatQueueComponent>(player))
                sEntMan.RemoveComponent<CombatQueueComponent>(player);
        });
        await RunTicks(1);

        await server.WaitPost(() =>
        {
            var ev = new QueueFireballActionEvent { Performer = player };
            sEntMan.EventBus.RaiseLocalEvent(player, ev, true);
        });
        await RunTicks(2);

        Assert.That(sEntMan.HasComponent<CombatQueueComponent>(player), Is.True);
        var queue = sEntMan.GetComponent<CombatQueueComponent>(player);
        Assert.Multiple(() =>
        {
            Assert.That(queue.Queue.Count, Is.EqualTo(1));
            Assert.That(queue.Queue[0].Kind, Is.EqualTo(QueuedActionKind.Fireball));
        });
    }

    private (Robust.UnitTesting.RobustIntegrationTest.ServerIntegrationInstance server,
             IEntityManager sEntMan,
             EntityUid player) Resolve()
    {
        var server = Pair.Server;
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var player = server.ResolveDependency<IPlayerManager>().Sessions.Single().AttachedEntity!.Value;
        return (server, sEntMan, player);
    }

    private async Task RunTicks(int count)
    {
        for (var i = 0; i < count; i++)
            await Pair.Server.WaitRunTicks(1);
    }
}

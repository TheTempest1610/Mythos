using Content.Shared.Mythos.Combat.Queue;
using Content.Shared.Mythos.Magic.Actions;

namespace Content.Server.Mythos.Magic.Actions;

/// <summary>
/// Bridge from the stock Actions hotbar to the Mythos combat queue. When a
/// player activates a spell action (Fireball / Magic Missile), this
/// translates the <c>InstantActionEvent</c> into a queue enqueue so the
/// spell rides the same cast pipeline as a programmatic
/// <c>CombatQueueSystem.TryEnqueue</c>. The queue stays the source of truth
/// for ordering, capacity, mana gating, and cast lifecycle; the hotbar is
/// just the user-facing entry point.
///
/// Actions are handled on the server only: enqueue writes authoritative
/// component state, which replicates back to the client for the (future)
/// queue widget. Optimistic-UI for enqueue-on-click can be layered in a
/// later polish pass if the RTT-visible delay is a problem in play.
/// </summary>
public sealed class SpellActionsSystem : EntitySystem
{
    [Dependency] private readonly SharedCombatQueueSystem _queue = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<QueueFireballActionEvent>(OnFireball);
        SubscribeLocalEvent<QueueMagicMissileActionEvent>(OnMagicMissile);
    }

    private void OnFireball(QueueFireballActionEvent ev)
    {
        if (ev.Handled)
            return;

        ev.Handled = _queue.TryEnqueue(ev.Performer, QueuedActionKind.Fireball, null);
    }

    private void OnMagicMissile(QueueMagicMissileActionEvent ev)
    {
        if (ev.Handled)
            return;

        ev.Handled = _queue.TryEnqueue(ev.Performer, QueuedActionKind.MagicMissile, null);
    }
}

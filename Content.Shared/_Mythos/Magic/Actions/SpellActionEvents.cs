using Content.Shared.Actions;

namespace Content.Shared.Mythos.Magic.Actions;

/// <summary>
/// Raised when the player activates the Fireball action on their hotbar.
/// The server-side <c>SpellActionsSystem</c> translates this into a queue
/// enqueue so Fireball flows through the same cast pipeline as a
/// programmatic <c>TryEnqueue</c>. Queue is the source of truth;
/// the hotbar button is just a convenient entry point.
/// </summary>
public sealed partial class QueueFireballActionEvent : InstantActionEvent
{
}

/// <summary>
/// Raised when the player activates the Magic Missile action on their
/// hotbar. Same pattern as <see cref="QueueFireballActionEvent"/>;
/// enqueues a <c>MagicMissile</c> kind against the current combat target.
/// </summary>
public sealed partial class QueueMagicMissileActionEvent : InstantActionEvent
{
}

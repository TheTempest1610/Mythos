using Content.Shared.Actions;
using Content.Shared.Mobs.Components;
using Content.Shared.Mythos.Magic.Mana;
using Robust.Shared.Player;

namespace Content.Server.Mythos.Player;

/// <summary>
/// Grants the default spell loadout (<see cref="ManaComponent"/> plus the
/// Magic Missile and Fireball action entities) to a player's mob the first
/// time they attach. Idempotent via <see cref="MythosSpellLoadoutComponent"/>
/// so reconnects and re-attaches don't stack duplicate actions on the
/// hotbar.
///
/// Gated to entities with <see cref="MobStateComponent"/>; ghosts and
/// admin observers attach to non-mob entities for which spell loadout is
/// irrelevant. The ManaComponent on a non-mob would be harmless, but the
/// hotbar actions would be noise.
///
/// This is the minimum viable flow: every living player gets both
/// spells free. A future wand-item / spellbook system would replace this
/// with item-granted actions via <c>GetItemActionsEvent</c>, at which point
/// this system can narrow to the mana-only grant, or be removed if mana
/// also becomes item-scoped.
/// </summary>
public sealed class MythosSpellLoadoutSystem : EntitySystem
{
    private const string FireballActionProtoId = "ActionMythosFireball";
    private const string MagicMissileActionProtoId = "ActionMythosMagicMissile";

    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        var entity = ev.Entity;

        if (HasComp<MythosSpellLoadoutComponent>(entity))
            return;

        if (!HasComp<MobStateComponent>(entity))
            return;

        EnsureComp<ManaComponent>(entity);
        _actions.AddAction(entity, MagicMissileActionProtoId);
        _actions.AddAction(entity, FireballActionProtoId);
        EnsureComp<MythosSpellLoadoutComponent>(entity);
    }
}

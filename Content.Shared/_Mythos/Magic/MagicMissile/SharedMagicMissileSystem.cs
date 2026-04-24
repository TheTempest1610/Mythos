using Content.Shared.Mythos.Combat.Events;
using Content.Shared.Mythos.Magic.Mana;
using Robust.Shared.Player;

namespace Content.Shared.Mythos.Magic.MagicMissile;

/// <summary>
/// Shared cast handler for Magic Missile. Responsible for the mana side of
/// the cast: both client and server run this handler, spending mana in
/// lockstep so the mana bar drops optimistically on the client the same
/// tick the cast fires. The server override adds damage application.
///
/// If <see cref="SharedManaSystem.TrySpend"/> reports insufficient mana, the
/// handler no-ops: no damage, no visual effect. The executor checks mana
/// up-front before firing so this guard is a belt-and-suspenders safety
/// against races; it should not normally trigger.
/// </summary>
public abstract class SharedMagicMissileSystem : EntitySystem
{
    /// <summary>
    /// Magic Missile's mana cost is hardcoded here for now. Per-spell costs
    /// will move into <c>SpellComponent.ManaCost</c> on the action prototype.
    /// </summary>
    public const float ManaCost = 10f;

    [Dependency] protected readonly SharedManaSystem Mana = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<CastMagicMissileEvent>(OnCast);
    }

    /// <summary>
    /// Base handler spends mana only. Server override adds damage. Virtual
    /// dispatch picks the right path per side: client runs the base, server
    /// runs the override.
    /// </summary>
    protected virtual void OnCast(CastMagicMissileEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } caster)
            return;

        Mana.TrySpend(caster, ManaCost);
    }
}

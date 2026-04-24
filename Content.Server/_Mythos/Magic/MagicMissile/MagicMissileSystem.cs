using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mythos.Combat.Events;
using Content.Shared.Mythos.Magic.MagicMissile;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Mythos.Magic.MagicMissile;

/// <summary>
/// Server-side Magic Missile handler. Builds a damage specifier at init time
/// and applies it to the target on every successful cast. Mana spending
/// shares the base-class flow; this override interleaves the spend with the
/// damage application so no damage is ever applied for a cast that failed
/// on mana cost.
///
/// Currently uses <c>Slash</c> as the damage type so the spell works against
/// the existing mob damage-resistance model without introducing new damage
/// type prototypes. A later polish pass should add an <c>Arcane</c> damage
/// type with proper resistances and retunes.
/// </summary>
public sealed class MagicMissileSystem : SharedMagicMissileSystem
{
    private const int MagicMissileDamage = 8;
    private const string MagicMissileDamageType = "Slash";

    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private DamageSpecifier? _damageSpec;

    public override void Initialize()
    {
        base.Initialize();
        BuildDamageSpec();
    }

    private void BuildDamageSpec()
    {
        if (!_proto.TryIndex<DamageTypePrototype>(MagicMissileDamageType, out _))
            return;

        _damageSpec = new DamageSpecifier();
        _damageSpec.DamageDict[MagicMissileDamageType] = FixedPoint2.New(MagicMissileDamage);
    }

    protected override void OnCast(CastMagicMissileEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } caster)
            return;

        // Spend mana atomically; short-circuit damage on failure.
        if (!Mana.TrySpend(caster, ManaCost))
            return;

        if (_damageSpec == null)
            return;

        if (!TryGetEntity(ev.Target, out var target))
            return;

        _damage.TryChangeDamage(target.Value, _damageSpec, origin: caster);
    }
}

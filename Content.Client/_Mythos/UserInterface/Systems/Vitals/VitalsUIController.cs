using Content.Client._Mythos.UserInterface.Screens;
using Content.Client._Mythos.UserInterface.Systems.Vitals.Widgets;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Client._Mythos.UserInterface.Systems.Vitals;

// Mythos: Drives the V2 HUD vitals strip. The HP orb is wired to the local player's
// DamageableComponent + MobThresholdsComponent (max HP = critical-or-dead incap
// threshold; current HP = max - total damage). The Qi orb still uses mock data
// pending a real Qi/mana component. Falls back to mock values whenever the player
// isn't attached to a body (chargen / ghost / pre-round).
[UsedImplicitly]
public sealed class VitalsUIController : UIController, IOnStateEntered<GameplayState>
{
    [Dependency] private readonly IPlayerManager _player = default!;

    [UISystemDependency] private readonly MobThresholdSystem _thresholds = default!;
    [UISystemDependency] private readonly DamageableSystem _damageable = default!;

    // Qi mock values stay until a real component lands.
    private const float MockQiCurrent = 756f;
    private const float MockQiMax = 812f;

    // HP fallback when the player isn't bound to a damageable body.
    private const float MockHpCurrent = 1284f;
    private const float MockHpMax = 1284f;

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += ApplyToActiveScreen;

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<MobThresholdChecked>(OnThresholdChecked);
    }

    public void OnStateEntered(GameplayState state)
    {
        ApplyToActiveScreen();
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        ApplyToActiveScreen();
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        ApplyToActiveScreen();
    }

    private void OnDamageChanged(DamageChangedEvent args)
    {
        if (args.Damageable.Owner != _player.LocalEntity)
            return;

        UpdateHp();
    }

    private void OnThresholdChecked(ref MobThresholdChecked args)
    {
        if (args.Target != _player.LocalEntity)
            return;

        UpdateHp();
    }

    private void ApplyToActiveScreen()
    {
        if (UIManager.ActiveScreen is not MythosGameScreen)
            return;

        UpdateHp();
        UpdateQi();
    }

    private void UpdateHp()
    {
        var orbs = UIManager.GetActiveUIWidgetOrNull<VitalsOrbsBar>();
        if (orbs is null)
            return;

        if (!TryReadPlayerHp(out var current, out var max))
        {
            orbs.SetHp(MockHpCurrent, MockHpMax);
            return;
        }

        orbs.SetHp(current, max);
    }

    private void UpdateQi()
    {
        var orbs = UIManager.GetActiveUIWidgetOrNull<VitalsOrbsBar>();
        orbs?.SetQi(MockQiCurrent, MockQiMax);
    }

    /// <summary>
    /// Reads HP for the local player from <see cref="DamageableComponent"/> and
    /// <see cref="MobThresholdsComponent"/>. Returns false (and leaves outputs
    /// untouched) if the player isn't currently attached to a damageable body.
    /// </summary>
    private bool TryReadPlayerHp(out float current, out float max)
    {
        current = 0f;
        max = 0f;

        var entity = _player.LocalEntity;
        if (entity is null)
            return false;

        if (!EntityManager.TryGetComponent<DamageableComponent>(entity, out var damageable))
            return false;

        if (!_thresholds.TryGetIncapThreshold(entity.Value, out var incap))
            return false;

        var maxHp = (FixedPoint2)incap;
        var damage = _damageable.GetTotalDamage((entity.Value, damageable));
        var hp = FixedPoint2.Max(FixedPoint2.Zero, maxHp - damage);

        current = hp.Float();
        max = maxHp.Float();
        return true;
    }
}

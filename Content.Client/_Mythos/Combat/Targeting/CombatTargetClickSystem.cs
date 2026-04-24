using Content.Client.Gameplay;
using Content.Shared.CombatMode;
using Content.Shared.Mythos.Combat.Events;
using Content.Shared.Mythos.Combat.Targeting;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Mythos.Combat.Targeting;

/// <summary>
/// Client-side observer that polls the <see cref="EngineKeyFunctions.Use"/> key for
/// target-select intent, independent of the melee weapon cooldown gate in
/// <c>MeleeWeaponSystem.Update</c>. When the player presses their primary button on a
/// valid mob while in combat mode, this raises a predictive
/// <see cref="SelectCombatTargetEvent"/> regardless of whether the player's weapon is
/// currently on cooldown, so retargeting during an active auto-attack loop is
/// instantaneous instead of silently dropped by the stock cooldown gate.
///
/// The stock melee click path in <c>MeleeWeaponSystem.ClientLightAttack</c> is left
/// completely untouched. If the click happens to coincide with a cooldown-clear
/// tick and the target is in range, the player also swings at the mob (NWN-style
/// engage-on-click). This is desirable: a retarget click doubles as a first swing.
/// </summary>
public sealed class CombatTargetClickSystem : SharedCombatTargetSystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;

    private BoundKeyState _lastUseState = BoundKeyState.Up;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var useState = _inputSystem.CmdStates.GetState(EngineKeyFunctions.Use);

        // Edge detection: only act on the Up → Down transition so that holding the
        // button doesn't spam select events every tick.
        var pressed = useState == BoundKeyState.Down && _lastUseState != BoundKeyState.Down;
        _lastUseState = useState;

        if (!pressed)
            return;

        if (_player.LocalEntity is not { } attacker)
            return;

        if (!_combatMode.IsInCombatMode(attacker))
            return;

        var mousePos = _eyeManager.PixelToMap(_input.MouseScreenPosition);
        if (mousePos.MapId == MapId.Nullspace)
            return;

        if (_stateManager.CurrentState is not GameplayStateBase screen)
            return;

        if (screen.GetClickedEntity(mousePos) is not { } target)
            return;

        if (!IsValidTarget(attacker, target))
            return;

        RaisePredictiveEvent(new SelectCombatTargetEvent(GetNetEntity(target)));
    }
}

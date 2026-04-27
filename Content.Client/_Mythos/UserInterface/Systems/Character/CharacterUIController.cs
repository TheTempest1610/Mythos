using Content.Client._Mythos.UserInterface.Screens;
using Content.Client._Mythos.UserInterface.Systems.Character.Widgets;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Gameplay;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Player;

namespace Content.Client._Mythos.UserInterface.Systems.Character;

// Mythos: V2 HUD character sidebar controller. Owns the always-visible portrait +
// equipment paper-doll. Pushes the local player entity into PortraitControl on
// attach/detach so the SpriteView renders the live character (locked to south
// facing). Distinct from upstream Content.Client.UserInterface.Systems.Character.
// CharacterUIController which owns the Character menu window.
[UsedImplicitly]
public sealed class CharacterUIController : UIController, IOnStateEntered<GameplayState>
{
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += ApplyToActiveScreen;

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
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
        var panel = UIManager.GetActiveUIWidgetOrNull<CharacterPanel>();
        panel?.PortraitControl.SetEntity(null);
    }

    private void ApplyToActiveScreen()
    {
        if (UIManager.ActiveScreen is not MythosGameScreen)
            return;

        var panel = UIManager.GetActiveUIWidgetOrNull<CharacterPanel>();
        panel?.PortraitControl.SetEntity(_player.LocalEntity);
    }
}

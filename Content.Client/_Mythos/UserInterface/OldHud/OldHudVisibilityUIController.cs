using Content.Client._Mythos.UserInterface.Screens;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Gameplay;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.Mythos.UserInterface.OldHud;

/// <summary>
/// Owns the transitional toggle for suppressing the inherited SS14 HUD while
/// Mythos builds a replacement UI.
/// </summary>
public sealed class OldHudVisibilityUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    public bool IsOldHudHidden { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += ApplyToActiveScreen;
    }

    public bool ToggleOldHud()
    {
        IsOldHudHidden = !IsOldHudHidden;
        ApplyToActiveScreen();
        return IsOldHudHidden;
    }

    public void OnStateEntered(GameplayState state)
    {
        ApplyToActiveScreen();
    }

    public void OnStateExited(GameplayState state)
    {
        ApplyOldHudVisible(true);
    }

    private void ApplyToActiveScreen()
    {
        // Mythos: V2 HUD ships its own equivalents for the upstream menu bar / hotbar /
        // alerts; default-hide the legacy overlay so they don't render on top. The
        // `hide_old_ui` console command can still flip back from there for debugging.
        if (UIManager.ActiveScreen is MythosGameScreen)
            IsOldHudHidden = true;

        ApplyOldHudVisible(!IsOldHudHidden);
    }

    private void ApplyOldHudVisible(bool visible)
    {
        if (UIManager.ActiveScreen is InGameScreen inGameScreen)
            inGameScreen.SetOldHudVisible(visible);
    }
}

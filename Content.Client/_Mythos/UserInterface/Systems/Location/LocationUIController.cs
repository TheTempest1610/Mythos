using Content.Client._Mythos.UserInterface.Screens;
using Content.Client._Mythos.UserInterface.Systems.Location.Widgets;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Gameplay;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._Mythos.UserInterface.Systems.Location;

// Mythos: Pushes hardcoded location and time strings into LocationTimeWidget.
// Real station/region + in-world clock subscriptions land post-mockup.
public sealed class LocationUIController : UIController, IOnStateEntered<GameplayState>
{
    private const string MockTime = "23:42";
    private const string MockRegion = "Peach Blossom Valley";
    private const string MockMoonGlyph = "☽";

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += ApplyToActiveScreen;
    }

    public void OnStateEntered(GameplayState state)
    {
        ApplyToActiveScreen();
    }

    private void ApplyToActiveScreen()
    {
        if (UIManager.ActiveScreen is not MythosGameScreen)
            return;

        var widget = UIManager.GetActiveUIWidgetOrNull<LocationTimeWidget>();
        widget?.SetLocation(MockTime, MockRegion, MockMoonGlyph);
    }
}

using Content.Client._Mythos.UserInterface.Screens;
using Content.Client._Mythos.UserInterface.Systems.Stats.Widgets;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Gameplay;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._Mythos.UserInterface.Systems.Stats;

// Mythos: Hardcoded character stats for the V2 mockup. Pushes values into any
// StatsPanel currently mounted. Real component subscriptions land post-mockup.
public sealed class StatsUIController : UIController, IOnStateEntered<GameplayState>
{
    public const string MockName = "Liu Xianyi";
    public const string MockClass = "Sword Sect Initiate";
    public const int MockLevel = 12;
    public const float MockXp = 4720f;
    public const float MockXpToNext = 8000f;

    public const float MockHp = 1284f;
    public const float MockQi = 756f;
    public const int MockAtk = 142;
    public const int MockDef = 88;
    public const int MockSpirit = 211;
    public const int MockDex = 167;

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

    public void Apply(StatsPanel panel)
    {
        panel.SetCharacter(MockName, MockClass, MockLevel, MockXp, MockXpToNext);
        panel.SetStats(MockHp, MockQi, MockAtk, MockDef, MockSpirit, MockDex);
    }

    private void ApplyToActiveScreen()
    {
        if (UIManager.ActiveScreen is not MythosGameScreen)
            return;

        var panel = UIManager.GetActiveUIWidgetOrNull<StatsPanel>();
        if (panel is not null)
            Apply(panel);
    }
}

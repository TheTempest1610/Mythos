using Content.Client._Mythos.UserInterface.Systems.Stats;
using Content.Client._Mythos.UserInterface.Systems.Stats.Widgets;
using Content.Client.UserInterface.Systems.Character.Windows;
using Robust.Client.UserInterface;
using Robust.Shared.IoC;

namespace Content.Client._Mythos.UserInterface.Systems.Character.Widgets;

// Mythos: Character window for the V2 HUD. Subclasses upstream CharacterWindow so
// the existing CharacterUIController.CharacterUpdated path keeps working (NameLabel,
// SpriteView, etc. still resolve via inherited XAML); the upstream content is hidden
// for the mockup and replaced by a StatsPanel showing hardcoded values.
//
// Upstream CharacterWindow had to be unsealed for this subclass; that was a single-
// line `// Mythos:` edit in CharacterWindow.xaml.cs.
public sealed class MythosCharacterWindow : CharacterWindow
{
    public StatsPanel Stats { get; }

    public MythosCharacterWindow()
    {
        // Hide upstream content so only the Mythos stats panel shows in V2 mode.
        RoleType.Visible = false;
        SpriteView.Visible = false;
        NameLabel.Visible = false;
        SubText.Visible = false;
        ObjectivesLabel.Visible = false;
        Objectives.Visible = false;
        RolePlaceholder.Visible = false;

        // Mount the Mythos stats panel as a sibling of the inherited content.
        // RoleType is inside `<ScrollContainer><BoxContainer Orientation="Vertical">...</BoxContainer></ScrollContainer>`,
        // so RoleType.Parent is the BoxContainer we want.
        Stats = new StatsPanel();
        var contentContainer = RoleType.Parent;
        contentContainer?.AddChild(Stats);

        // Pre-populate with hardcoded mock data so the window is screenshot-ready.
        var stats = IoCManager.Resolve<IUserInterfaceManager>().GetUIController<StatsUIController>();
        stats.Apply(Stats);

        Title = "Character";
        MinWidth = 400;
        MinHeight = 480;
    }
}

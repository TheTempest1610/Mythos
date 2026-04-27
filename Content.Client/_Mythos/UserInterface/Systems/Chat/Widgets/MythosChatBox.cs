using Content.Client._Mythos.UserInterface.Stylesheets;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Robust.Shared.IoC;

namespace Content.Client._Mythos.UserInterface.Systems.Chat.Widgets;

// Mythos: Chat box subclass for the V2 HUD. Reuses upstream ChatBox XAML and behaviour
// (so ChatUIController auto-registration in the base ctor still works); adds the Mythos
// frame style class to the inherited ChatWindowPanel for cyan theming.
public sealed class MythosChatBox : ChatBox
{
    public MythosChatBox()
    {
        IoCManager.InjectDependencies(this);

        ChatWindowPanel.AddStyleClass(MythosPalette.FrameClass);
    }
}

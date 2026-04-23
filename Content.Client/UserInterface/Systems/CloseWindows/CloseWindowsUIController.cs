using System.Linq;
using Content.Client.UserInterface.Controls;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.CustomControls;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Systems.CloseWindows;

[UsedImplicitly]
public sealed class CloseWindowsUIController : UIController
{
    private MenuButton? CloseWindowsButton =>
        UIManager.GetActiveUIWidgetOrNull<MenuBar.Widgets.GameTopMenuBar>()?.CloseWindowsButton;

    public void LoadButton()
    {
        if (CloseWindowsButton == null)
            return;

        CloseWindowsButton.OnPressed += CloseWindowsButtonPressed;
    }

    public void UnloadButton()
    {
        if (CloseWindowsButton == null)
            return;

        CloseWindowsButton.OnPressed -= CloseWindowsButtonPressed;
    }

    private void CloseWindowsButtonPressed(ButtonEventArgs args)
    {
        foreach (var window in UIManager.WindowRoot.Children.OfType<BaseWindow>().ToList())
        {
            window.Dispose();
        }

        if (CloseWindowsButton != null)
            CloseWindowsButton.Pressed = false;
    }
}

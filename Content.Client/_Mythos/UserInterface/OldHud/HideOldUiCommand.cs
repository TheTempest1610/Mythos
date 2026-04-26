using Content.Shared.Administration;
using Robust.Client.UserInterface;
using Robust.Shared.Console;

namespace Content.Client.Mythos.UserInterface.OldHud;

[AnyCommand]
public sealed class HideOldUiCommand : IConsoleCommand
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    public string Command => "hide_old_ui";
    public string Description => "Toggles visibility of the inherited in-game HUD.";
    public string Help => "Usage: hide_old_ui";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0)
        {
            shell.WriteLine(Help);
            return;
        }

        var controller = _ui.GetUIController<OldHudVisibilityUIController>();
        var hidden = controller.ToggleOldHud();
        shell.WriteLine(hidden ? "Old in-game UI hidden." : "Old in-game UI shown.");
    }
}

using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Stylesheets;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._Mythos.UserInterface.Stylesheets;

// Mythos: Top-bar tab restyle for the V2 HUD. Pure stylesheet pass on the upstream
// GameTopMenuBar - text labels stay (icons deferred until art lands), button states
// pick up the Mythos cyan palette via FontColor and Modulate overrides.
[CommonSheetlet]
public sealed class MythosTabSheetlet : Sheetlet<NanotrasenStylesheet>
{
    public override StyleRule[] GetRules(NanotrasenStylesheet sheet, object config)
    {
        return
        [
            // Apply Mythos cyan to MenuButton labels regardless of state.
            E<MenuButton>()
                .ParentOf(E<Label>())
                .FontColor(MythosPalette.MoonWhite),

            E<MenuButton>().Pseudo(ContainerButton.StylePseudoClassHover)
                .ParentOf(E<Label>())
                .FontColor(MythosPalette.AccentStrong),

            E<MenuButton>().Pseudo(ContainerButton.StylePseudoClassPressed)
                .ParentOf(E<Label>())
                .FontColor(MythosPalette.Accent),
        ];
    }
}

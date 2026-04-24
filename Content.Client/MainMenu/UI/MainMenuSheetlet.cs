using Content.Client.Changelog;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.MainMenu.UI;

[CommonSheetlet]
public sealed class MainMenuSheetlet : Sheetlet<NanotrasenStylesheet>
{
    public override StyleRule[] GetRules(NanotrasenStylesheet sheet, object config)
    {
        var transparentButton = new StyleBoxEmpty();

        return
        [
            E<Button>()
                .Identifier(MainMenuControl.StyleIdentifierMainMenu)
                .PseudoNormal()
                .Prop(Button.StylePropertyStyleBox, transparentButton),
            E<Button>()
                .Identifier(MainMenuControl.StyleIdentifierMainMenu)
                .PseudoHovered()
                .Prop(Button.StylePropertyStyleBox, transparentButton),
            E<Button>()
                .Identifier(MainMenuControl.StyleIdentifierMainMenu)
                .PseudoPressed()
                .Prop(Button.StylePropertyStyleBox, transparentButton),
            E<Button>()
                .Identifier(MainMenuControl.StyleIdentifierMainMenu)
                .PseudoDisabled()
                .Prop(Button.StylePropertyStyleBox, transparentButton),
            E<ChangelogButton>()
                .Identifier(MainMenuControl.StyleIdentifierMainMenu)
                .PseudoNormal()
                .Prop(Button.StylePropertyStyleBox, transparentButton),
            E<ChangelogButton>()
                .Identifier(MainMenuControl.StyleIdentifierMainMenu)
                .PseudoHovered()
                .Prop(Button.StylePropertyStyleBox, transparentButton),
            E<ChangelogButton>()
                .Identifier(MainMenuControl.StyleIdentifierMainMenu)
                .PseudoPressed()
                .Prop(Button.StylePropertyStyleBox, transparentButton),
            E<ChangelogButton>()
                .Identifier(MainMenuControl.StyleIdentifierMainMenu)
                .PseudoDisabled()
                .Prop(Button.StylePropertyStyleBox, transparentButton),
            E<Button>()
                .Identifier(MainMenuControl.StyleIdentifierMainMenu)
                .ParentOf(E<Label>())
                .Font(sheet.BaseFont.GetFont(16, FontKind.Bold)),
            E<ChangelogButton>()
                .Identifier(MainMenuControl.StyleIdentifierMainMenu)
                .ParentOf(E<Label>())
                .Font(sheet.BaseFont.GetFont(16, FontKind.Bold)),
            E<BoxContainer>()
                .Identifier(MainMenuControl.StyleIdentifierMainMenuVBox)
                .Prop(BoxContainer.StylePropertySeparation, 2),
        ];
    }
}

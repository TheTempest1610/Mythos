using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._Mythos.UserInterface.Stylesheets;

// Mythos: Hotbar slot styling for the V2 HUD. Restyle pass on the upstream HotbarGui
// slot buttons; full 9-slice frame swap is deferred to art-ready phase. For now this
// just tints slots when they carry the MythosHotbarSlot class.
[CommonSheetlet]
public sealed class MythosHotbarSheetlet : Sheetlet<NanotrasenStylesheet>
{
    public override StyleRule[] GetRules(NanotrasenStylesheet sheet, object config)
    {
        return
        [
            E<TextureRect>()
                .Class(MythosPalette.HotbarSlotClass)
                .Modulate(MythosPalette.EdgeBright),
        ];
    }
}

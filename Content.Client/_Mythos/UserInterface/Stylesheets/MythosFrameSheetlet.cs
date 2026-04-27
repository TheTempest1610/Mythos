using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._Mythos.UserInterface.Stylesheets;

// Mythos: Frame chrome rules for the V2 HUD. Defines flat-fill panel styles
// (Phase 2 placeholder; can be swapped to StyleBoxTexture 9-slice later)
// for sidebars, headers, dividers, and the top-right location pill.
[CommonSheetlet]
public sealed class MythosFrameSheetlet : Sheetlet<NanotrasenStylesheet>
{
    public override StyleRule[] GetRules(NanotrasenStylesheet sheet, object config)
    {
        var framePanel = new StyleBoxFlat
        {
            BackgroundColor = MythosPalette.Surface.WithAlpha(0.92f),
            BorderColor = MythosPalette.Edge.WithAlpha(0.85f),
            BorderThickness = new Thickness(1),
        };
        framePanel.SetContentMarginOverride(StyleBox.Margin.All, 8);

        var headerPanel = new StyleBoxFlat
        {
            BackgroundColor = MythosPalette.AccentDim,
            BorderColor = MythosPalette.EdgeBright.WithAlpha(0.85f),
            BorderThickness = new Thickness(0, 0, 0, 2),
        };
        headerPanel.SetContentMarginOverride(StyleBox.Margin.Horizontal, 8);
        headerPanel.SetContentMarginOverride(StyleBox.Margin.Vertical, 4);

        var dividerPanel = new StyleBoxFlat
        {
            BackgroundColor = MythosPalette.Edge.WithAlpha(0.5f),
        };

        var locationPillPanel = new StyleBoxFlat
        {
            BackgroundColor = MythosPalette.SurfaceRaised.WithAlpha(0.9f),
            BorderColor = MythosPalette.EdgeBright.WithAlpha(0.7f),
            BorderThickness = new Thickness(1),
        };
        locationPillPanel.SetContentMarginOverride(StyleBox.Margin.Horizontal, 10);
        locationPillPanel.SetContentMarginOverride(StyleBox.Margin.Vertical, 4);

        return
        [
            E<PanelContainer>()
                .Class(MythosPalette.FrameClass)
                .Panel(framePanel),
            E<PanelContainer>()
                .Class(MythosPalette.HeaderClass)
                .Panel(headerPanel),
            E<PanelContainer>()
                .Class(MythosPalette.DividerClass)
                .Panel(dividerPanel)
                .MinHeight(1),
            E<PanelContainer>()
                .Class(MythosPalette.LocationPillClass)
                .Panel(locationPillPanel),

            // Default text colors inside Mythos frames.
            E<PanelContainer>()
                .Class(MythosPalette.FrameClass)
                .ParentOf(E<Label>())
                .FontColor(MythosPalette.MoonWhite),
            E<PanelContainer>()
                .Class(MythosPalette.LocationPillClass)
                .ParentOf(E<Label>())
                .FontColor(MythosPalette.MoonWhite),
        ];
    }
}

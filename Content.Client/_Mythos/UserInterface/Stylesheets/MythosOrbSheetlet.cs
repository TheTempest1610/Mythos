using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._Mythos.UserInterface.Stylesheets;

// Mythos: Orb visual rules for the V2 HUD vitals strip. Phase 2 uses StyleBoxFlat
// rectangles; the V2 plan calls for vertical-fill TextureProgressBar with circular
// fg/bg textures, which is wired in VitalsOrbsBar at the widget level (modulate via
// these style classes for tinting).
[CommonSheetlet]
public sealed class MythosOrbSheetlet : Sheetlet<NanotrasenStylesheet>
{
    public override StyleRule[] GetRules(NanotrasenStylesheet sheet, object config)
    {
        var orbBackplate = new StyleBoxFlat
        {
            BackgroundColor = MythosPalette.Ink.WithAlpha(0.85f),
            BorderColor = MythosPalette.EdgeBright.WithAlpha(0.6f),
            BorderThickness = new Thickness(2),
        };
        orbBackplate.SetContentMarginOverride(StyleBox.Margin.All, 4);

        var orbGlow = new StyleBoxFlat
        {
            BackgroundColor = Color.Transparent,
            BorderColor = MythosPalette.EdgeBright.WithAlpha(0.45f),
            BorderThickness = new Thickness(1),
        };

        // Empty backing for the orb fill (so the unfilled portion is dark).
        var orbFillBackground = new StyleBoxFlat
        {
            BackgroundColor = MythosPalette.Surface.WithAlpha(0.6f),
        };

        var hpFillForeground = new StyleBoxFlat
        {
            BackgroundColor = MythosPalette.HpFill,
            BorderColor = MythosPalette.AccentStrong,
            BorderThickness = new Thickness(1),
        };

        var qiFillForeground = new StyleBoxFlat
        {
            BackgroundColor = MythosPalette.QiFill,
            BorderColor = MythosPalette.MoonWhite,
            BorderThickness = new Thickness(1),
        };

        return
        [
            E<PanelContainer>()
                .Class(MythosPalette.OrbBackplateClass)
                .Panel(orbBackplate),
            E<PanelContainer>()
                .Class(MythosPalette.OrbGlowClass)
                .Panel(orbGlow),

            // ProgressBar background + foreground so vertical fill actually renders.
            E<ProgressBar>()
                .Class(MythosPalette.OrbFillHpClass)
                .Prop(ProgressBar.StylePropertyBackground, orbFillBackground)
                .Prop(ProgressBar.StylePropertyForeground, hpFillForeground),
            E<ProgressBar>()
                .Class(MythosPalette.OrbFillQiClass)
                .Prop(ProgressBar.StylePropertyBackground, orbFillBackground)
                .Prop(ProgressBar.StylePropertyForeground, qiFillForeground),
        ];
    }
}

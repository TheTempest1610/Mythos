using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.Stylesheets;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.MainMenu.UI;

[CommonSheetlet]
public sealed class MythosThemeSheetlet : Sheetlet<NanotrasenStylesheet>
{
    private const string ConnectingPanelId = "mythosConnectingPanel";
    private const string ConnectingTitleId = "mythosConnectingTitle";
    private const string ConnectingProgressBarId = "mythosConnectingProgressBar";

    private const string OptionsWindowId = "mythosOptionsWindow";
    private const string OptionsHeaderId = "mythosOptionsHeader";
    private const string OptionsTitleId = "mythosOptionsTitle";
    private const string OptionsCloseButtonId = "mythosOptionsCloseButton";
    private const string OptionsContentsId = "mythosOptionsContents";
    private const string OptionsBodyPanelId = "mythosOptionsBodyPanel";
    private const string OptionsTabsId = "mythosOptionsTabs";
    private const string OptionsRowLabelId = "mythosOptionsRowLabel";
    private const string OptionsDropdownId = "mythosOptionsDropdown";
    private const string OptionsSliderId = "mythosOptionsSlider";
    private const string OptionsValueLabelId = "mythosOptionsValueLabel";
    private const string OptionsActionBarId = "mythosOptionsActionBar";
    private const string OptionsActionButtonId = "mythosOptionsActionButton";
    private const string OptionsDangerButtonId = "mythosOptionsDangerButton";

    public override StyleRule[] GetRules(NanotrasenStylesheet sheet, object config)
    {
        var moonWhite = Color.FromHex("#F2F6FF");
        var mist = Color.FromHex("#B7C6D9");
        var accent = Color.FromHex("#5AD8FF");
        var accentStrong = Color.FromHex("#8CE7FF");
        var accentDim = Color.FromHex("#27495F");
        var ink = Color.FromHex("#0D131C");
        var surface = Color.FromHex("#161E29");
        var surfaceRaised = Color.FromHex("#1E2A38");
        var surfaceMuted = Color.FromHex("#223243");
        var edge = Color.FromHex("#33556D");
        var edgeBright = Color.FromHex("#67CFEF");
        var danger = Color.FromHex("#5A2D34");
        var dangerHover = Color.FromHex("#6B3640");
        var dangerPressed = Color.FromHex("#4B242A");

        var connectingPanel = new StyleBoxFlat
        {
            BackgroundColor = surface,
            BorderColor = edgeBright.WithAlpha(0.75f),
            BorderThickness = new Thickness(2),
        };
        connectingPanel.SetContentMarginOverride(StyleBox.Margin.All, 12);

        var progressBackground = new StyleBoxFlat
        {
            BackgroundColor = ink,
            BorderColor = edge,
            BorderThickness = new Thickness(1),
        };
        progressBackground.SetContentMarginOverride(StyleBox.Margin.Vertical, 12);

        var progressForeground = new StyleBoxFlat
        {
            BackgroundColor = accent,
        };
        progressForeground.SetContentMarginOverride(StyleBox.Margin.Vertical, 12);

        var optionsWindowPanel = new StyleBoxFlat
        {
            BackgroundColor = surface.WithAlpha(0.98f),
            BorderColor = edgeBright.WithAlpha(0.4f),
            BorderThickness = new Thickness(2),
        };
        optionsWindowPanel.SetContentMarginOverride(StyleBox.Margin.All, 10);

        var optionsWindowHeader = new StyleBoxFlat
        {
            BackgroundColor = ink.WithAlpha(0.98f),
            BorderColor = edgeBright.WithAlpha(0.55f),
            BorderThickness = new Thickness(0, 0, 0, 2),
        };
        optionsWindowHeader.SetContentMarginOverride(StyleBox.Margin.All, 8);

        var optionsBodyPanel = new StyleBoxFlat
        {
            BackgroundColor = surfaceRaised.WithAlpha(0.96f),
            BorderColor = edge.WithAlpha(0.9f),
            BorderThickness = new Thickness(1),
        };
        optionsBodyPanel.SetContentMarginOverride(StyleBox.Margin.All, 6);

        var optionsTabsPanel = new StyleBoxFlat
        {
            BackgroundColor = surface.WithAlpha(0.55f),
            BorderColor = edge.WithAlpha(0.7f),
            BorderThickness = new Thickness(1),
        };
        optionsTabsPanel.SetContentMarginOverride(StyleBox.Margin.All, 6);

        var optionsTabActive = new StyleBoxFlat
        {
            BackgroundColor = accentDim,
            BorderColor = edgeBright,
            BorderThickness = new Thickness(0, 0, 0, 2),
        };
        optionsTabActive.SetContentMarginOverride(StyleBox.Margin.Horizontal, 8);
        optionsTabActive.SetContentMarginOverride(StyleBox.Margin.Vertical, 3);

        var optionsTabInactive = new StyleBoxFlat
        {
            BackgroundColor = surface.WithAlpha(0.85f),
            BorderColor = edge.WithAlpha(0.55f),
            BorderThickness = new Thickness(0, 0, 0, 1),
        };
        optionsTabInactive.SetContentMarginOverride(StyleBox.Margin.Horizontal, 8);
        optionsTabInactive.SetContentMarginOverride(StyleBox.Margin.Vertical, 3);

        var stripeBack = new StyleBoxFlat
        {
            BackgroundColor = ink.WithAlpha(0.95f),
            BorderColor = edge.WithAlpha(0.7f),
            BorderThickness = new Thickness(0, 1, 0, 0),
        };
        stripeBack.SetContentMarginOverride(StyleBox.Margin.All, 4);

        var buttonNormal = CreateButtonBox(surfaceMuted, edge);
        var buttonHover = CreateButtonBox(accentDim, edgeBright);
        var buttonPressed = CreateButtonBox(Color.FromHex("#17394B"), accentStrong);
        var buttonDisabled = CreateButtonBox(surface, edge.WithAlpha(0.35f));

        var dangerButtonNormal = CreateButtonBox(danger, Color.FromHex("#9B5661"));
        var dangerButtonHover = CreateButtonBox(dangerHover, Color.FromHex("#C27381"));
        var dangerButtonPressed = CreateButtonBox(dangerPressed, Color.FromHex("#8F4E5A"));

        var sliderBack = new StyleBoxFlat
        {
            BackgroundColor = ink,
            BorderColor = edge,
            BorderThickness = new Thickness(1),
        };
        sliderBack.SetContentMarginOverride(StyleBox.Margin.Vertical, 6);

        var sliderFill = new StyleBoxFlat
        {
            BackgroundColor = accent,
        };
        sliderFill.SetContentMarginOverride(StyleBox.Margin.Vertical, 6);

        var sliderFore = new StyleBoxFlat
        {
            BackgroundColor = Color.Transparent,
            BorderColor = edgeBright.WithAlpha(0.65f),
            BorderThickness = new Thickness(1),
        };
        sliderFore.SetContentMarginOverride(StyleBox.Margin.Vertical, 6);

        var sliderGrab = new StyleBoxFlat
        {
            BackgroundColor = moonWhite,
            BorderColor = edgeBright,
            BorderThickness = new Thickness(1),
        };
        sliderGrab.SetContentMarginOverride(StyleBox.Margin.All, 8);

        return
        [
            E<PanelContainer>()
                .Identifier(ConnectingPanelId)
                .Panel(connectingPanel),
            E<Label>()
                .Identifier(ConnectingTitleId)
                .Font(sheet.BaseFont.GetFont(16, FontKind.Bold))
                .FontColor(accent),
            E<PanelContainer>()
                .Identifier(ConnectingPanelId)
                .ParentOf(E<Label>())
                .FontColor(moonWhite),
            E<PanelContainer>()
                .Identifier(ConnectingPanelId)
                .ParentOf(E<Label>().Class("LabelSubText"))
                .FontColor(mist),
            E<ProgressBar>()
                .Identifier(ConnectingProgressBarId)
                .Prop(ProgressBar.StylePropertyBackground, progressBackground)
                .Prop(ProgressBar.StylePropertyForeground, progressForeground),

            E<DefaultWindow>()
                .Identifier(OptionsWindowId)
                .Panel(optionsWindowPanel),
            E<PanelContainer>()
                .Identifier(OptionsHeaderId)
                .Panel(optionsWindowHeader),
            E<Label>()
                .Identifier(OptionsTitleId)
                .Font(sheet.BaseFont.GetFont(16, FontKind.Bold))
                .FontColor(accent),
            E<TextureButton>()
                .Identifier(OptionsCloseButtonId)
                .PseudoNormal()
                .Modulate(mist),
            E<TextureButton>()
                .Identifier(OptionsCloseButtonId)
                .PseudoHovered()
                .Modulate(accent),
            E<TextureButton>()
                .Identifier(OptionsCloseButtonId)
                .PseudoPressed()
                .Modulate(accentStrong),
            E()
                .Identifier(OptionsContentsId)
                .ParentOf(E<Label>())
                .FontColor(moonWhite),
            E()
                .Identifier(OptionsContentsId)
                .ParentOf(E<Label>().Class("LabelSubText"))
                .FontColor(mist),
            E()
                .Identifier(OptionsContentsId)
                .ParentOf(E<Label>().Class("LabelKeyText"))
                .FontColor(accent),
            E<PanelContainer>()
                .Identifier(OptionsBodyPanelId)
                .Panel(optionsBodyPanel),
            E<TabContainer>()
                .Identifier(OptionsTabsId)
                .Prop(TabContainer.StylePropertyPanelStyleBox, optionsTabsPanel)
                .Prop(TabContainer.StylePropertyTabStyleBox, optionsTabActive)
                .Prop(TabContainer.StylePropertyTabStyleBoxInactive, optionsTabInactive),
            E<TabContainer>()
                .Identifier(OptionsTabsId)
                .ParentOf(E<TextureRect>().Class(CheckBox.StyleClassCheckBox))
                .Modulate(accent),
            E<TabContainer>()
                .Identifier(OptionsTabsId)
                .ParentOf(E<TextureRect>().Class(OptionButton.StyleClassOptionTriangle))
                .Modulate(accent),
            E<StripeBack>()
                .Identifier(OptionsActionBarId)
                .Prop(StripeBack.StylePropertyBackground, stripeBack),
            E<Button>()
                .Identifier(OptionsActionButtonId)
                .PseudoNormal()
                .Box(buttonNormal),
            E<Button>()
                .Identifier(OptionsActionButtonId)
                .PseudoHovered()
                .Box(buttonHover),
            E<Button>()
                .Identifier(OptionsActionButtonId)
                .PseudoPressed()
                .Box(buttonPressed),
            E<Button>()
                .Identifier(OptionsActionButtonId)
                .PseudoDisabled()
                .Box(buttonDisabled)
                .Modulate(Color.White.WithAlpha(0.7f)),
            E<Button>()
                .Identifier(OptionsDangerButtonId)
                .PseudoNormal()
                .Box(dangerButtonNormal),
            E<Button>()
                .Identifier(OptionsDangerButtonId)
                .PseudoHovered()
                .Box(dangerButtonHover),
            E<Button>()
                .Identifier(OptionsDangerButtonId)
                .PseudoPressed()
                .Box(dangerButtonPressed),
            E<Label>()
                .Identifier(OptionsRowLabelId)
                .FontColor(moonWhite),
            E<Label>()
                .Identifier(OptionsValueLabelId)
                .FontColor(mist),
            E<OptionButton>()
                .Identifier(OptionsDropdownId)
                .PseudoNormal()
                .Box(buttonNormal),
            E<OptionButton>()
                .Identifier(OptionsDropdownId)
                .PseudoHovered()
                .Box(buttonHover),
            E<OptionButton>()
                .Identifier(OptionsDropdownId)
                .PseudoPressed()
                .Box(buttonPressed),
            E<OptionButton>()
                .Identifier(OptionsDropdownId)
                .PseudoDisabled()
                .Box(buttonDisabled),
            E<OptionButton>()
                .Identifier(OptionsDropdownId)
                .ParentOf(E<Label>().Class(OptionButton.StyleClassOptionButton))
                .FontColor(moonWhite),
            E<Slider>()
                .Identifier(OptionsSliderId)
                .Prop(Slider.StylePropertyBackground, sliderBack)
                .Prop(Slider.StylePropertyForeground, sliderFore)
                .Prop(Slider.StylePropertyFill, sliderFill)
                .Prop(Slider.StylePropertyGrabber, sliderGrab),
        ];
    }

    private static StyleBoxFlat CreateButtonBox(Color background, Color border)
    {
        var box = new StyleBoxFlat
        {
            BackgroundColor = background,
            BorderColor = border,
            BorderThickness = new Thickness(1),
        };

        box.SetContentMarginOverride(StyleBox.Margin.Horizontal, 12);
        box.SetContentMarginOverride(StyleBox.Margin.Vertical, 4);
        return box;
    }
}

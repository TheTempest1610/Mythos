using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Mythos.UserInterface.Equipment;

public sealed class MythosEquipmentPanel : Control
{
    private const float BackgroundSourceWidth = 1984f;
    private const float BackgroundSourceHeight = 4190f;
    private const float BackgroundAspect = BackgroundSourceWidth / BackgroundSourceHeight;
    private const float VerticalMargin = 12f;
    // Mythos: cap chrome height so the side bar renders at a consistent size at any
    // window resolution. Content children then have stable relative size + position
    // inside the chrome instead of drifting as the window scales.
    private const float MaxPanelHeight = 800f;
    private const string CollapseButtonInactivePath = "/Textures/UI/Equipment_Collapse_Inactive.png";
    private const string CollapseButtonActivePath = "/Textures/UI/Equipment_Collapse_Active.png";
    private static readonly Vector2 CollapseButtonSize = new(42f, 118f);

    private readonly TextureRect _background;
    private readonly TextureButton _collapseButton;
    private bool _collapsed;

    public float CoveredWidth { get; private set; }

    public MythosEquipmentPanel()
    {
        MouseFilter = MouseFilterMode.Ignore;

        _background = new TextureRect
        {
            TexturePath = "/Textures/UI/EquipmentBG.png",
            Stretch = TextureRect.StretchMode.Scale,
            CanShrink = true,
            MouseFilter = MouseFilterMode.Ignore
        };

        _collapseButton = new TextureButton
        {
            TexturePath = CollapseButtonInactivePath,
            MinSize = CollapseButtonSize,
            MouseFilter = MouseFilterMode.Stop
        };

        _collapseButton.OnMouseEntered += _ => _collapseButton.TexturePath = CollapseButtonActivePath;
        _collapseButton.OnMouseExited += _ => _collapseButton.TexturePath = CollapseButtonInactivePath;
        _collapseButton.OnButtonDown += _ => _collapseButton.TexturePath = CollapseButtonActivePath;
        _collapseButton.OnButtonUp += _ =>
        {
            _collapseButton.TexturePath = _collapseButton.IsHovered
                ? CollapseButtonActivePath
                : CollapseButtonInactivePath;
        };
        _collapseButton.OnPressed += _ => ToggleCollapsed();

        AddChild(_background);
        AddChild(_collapseButton);
    }

    private void ToggleCollapsed()
    {
        _collapsed = !_collapsed;
        _background.Visible = !_collapsed;
        InvalidateArrange();
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        _background.Measure(availableSize);
        _collapseButton.Measure(CollapseButtonSize);

        // Mythos: also measure non-chrome children so they can be hosted inside the
        // equipment panel's content area (e.g. portrait + 19-slot grid in the V2 HUD).
        foreach (var child in Children)
        {
            if (child == _background || child == _collapseButton)
                continue;

            child.Measure(availableSize);
        }

        return Vector2.Zero;
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var panelHeight = MathF.Min(MaxPanelHeight, MathF.Max(0f, finalSize.Y - VerticalMargin * 2f));
        var panelWidth = panelHeight * BackgroundAspect;
        var panelTop = (finalSize.Y - panelHeight) * 0.5f;
        CoveredWidth = _collapsed ? 0f : panelWidth;

        _background.Arrange(UIBox2.FromDimensions(
            new Vector2(0f, panelTop),
            new Vector2(panelWidth, panelHeight)));

        var buttonX = _collapsed ? 0f : MathF.Max(0f, panelWidth - CollapseButtonSize.X * 0.5f);
        var buttonY = (finalSize.Y - CollapseButtonSize.Y) * 0.5f;
        _collapseButton.Arrange(UIBox2.FromDimensions(
            new Vector2(buttonX, buttonY),
            CollapseButtonSize));

        // Mythos: arrange non-chrome children inside the panel's content rectangle.
        // Inset proportionally to the panel so layout is anchored to the chrome and
        // scales with it (rather than depending on absolute pixel margins). The top
        // inset clears the decorative "EQUIPMENT" label; the bottom inset is small
        // so content (e.g. portrait) hugs the bottom edge of the visible frame.
        var contentInsetX = panelWidth * 0.12f;
        var contentInsetTop = panelHeight * 0.16f;
        var contentInsetBottom = panelHeight * 0.04f;
        var contentLeft = contentInsetX;
        var contentTop = panelTop + contentInsetTop;
        var contentSize = new Vector2(
            MathF.Max(0f, panelWidth - contentInsetX * 2f),
            MathF.Max(0f, panelHeight - contentInsetTop - contentInsetBottom));

        foreach (var child in Children)
        {
            if (child == _background || child == _collapseButton)
                continue;

            child.Visible = !_collapsed;
            child.Arrange(UIBox2.FromDimensions(new Vector2(contentLeft, contentTop), contentSize));
        }

        return finalSize;
    }
}

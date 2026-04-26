using System.Numerics;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.Mythos.UserInterface.Chat;

public sealed class MythosChatPanel : Control
{
    private const float EquipmentSourceWidth = 1984f;
    private const float EquipmentSourceHeight = 4190f;
    private const float EquipmentAspect = EquipmentSourceWidth / EquipmentSourceHeight;
    private const float BorderSourceWidth = 100f;
    private const float BorderSourceHeight = 4190f;
    private const float BorderAspect = BorderSourceWidth / BorderSourceHeight;
    private const float EdgeCornerSourceWidth = 992f;
    private const float EdgeCenterSourceWidth = 1040f;
    private const float EdgeSourceHeight = 71f;
    private const float EdgeCornerAspect = EdgeCornerSourceWidth / EdgeSourceHeight;
    private const float EdgeCenterOverlapSourceWidth = (EdgeCenterSourceWidth - EdgeCornerSourceWidth) * 0.5f;
    private const float TitleSourceWidth = 1984f;
    private const float TitleSourceHeight = 577f;
    private const float TitleAspect = TitleSourceWidth / TitleSourceHeight;
    private const float VerticalMargin = 12f;
    private const float ContentPadding = 8f;
    private const float ResizeHandleWidth = 14f;
    private const float MaxWidthMultiplier = 2f;
    private const string CollapseButtonInactivePath = "/Textures/UI/Chat_Collapse_Inactive.png";
    private const string CollapseButtonActivePath = "/Textures/UI/Chat_Collapse_Active.png";
    private static readonly Vector2 CollapseButtonSize = new(42f, 118f);

    private readonly TiledBackground _background;
    private readonly TextureRect _leftBorder;
    private readonly TextureRect _topLeftCorner;
    private readonly TextureRect _topCenter;
    private readonly TextureRect _topRightCorner;
    private readonly TextureRect _bottomLeftCorner;
    private readonly TextureRect _bottomCenter;
    private readonly TextureRect _bottomRightCorner;
    private readonly TextureRect _rightBorder;
    private readonly TextureRect _titlePanel;
    private readonly ResizeHandle _resizeHandle;
    private readonly TextureButton _collapseButton;
    private float _widthMultiplier = 1f;
    private bool _collapsed;

    public UIBox2 PanelBounds { get; private set; }

    public MythosChatPanel()
    {
        MouseFilter = MouseFilterMode.Ignore;

        _background = new TiledBackground();

        _leftBorder = new TextureRect
        {
            TexturePath = "/Textures/UI/Chat_Border_L.png",
            Stretch = TextureRect.StretchMode.Scale,
            CanShrink = true,
            MouseFilter = MouseFilterMode.Ignore
        };

        _topLeftCorner = new TextureRect
        {
            TexturePath = "/Textures/UI/Chat_Border_Fill_Top_Corner_L.png",
            Stretch = TextureRect.StretchMode.Scale,
            CanShrink = true,
            MouseFilter = MouseFilterMode.Ignore
        };

        _topCenter = new TextureRect
        {
            TexturePath = "/Textures/UI/Chat_Border_Fill_Top_Center.png",
            Stretch = TextureRect.StretchMode.Scale,
            CanShrink = true,
            MouseFilter = MouseFilterMode.Ignore
        };

        _topRightCorner = new TextureRect
        {
            TexturePath = "/Textures/UI/Chat_Border_Fill_Top_Corner_R.png",
            Stretch = TextureRect.StretchMode.Scale,
            CanShrink = true,
            MouseFilter = MouseFilterMode.Ignore
        };

        _bottomLeftCorner = new TextureRect
        {
            TexturePath = "/Textures/UI/Chat_Border_Fill_Bottom_Corner_L.png",
            Stretch = TextureRect.StretchMode.Scale,
            CanShrink = true,
            MouseFilter = MouseFilterMode.Ignore
        };

        _bottomCenter = new TextureRect
        {
            TexturePath = "/Textures/UI/Chat_Border_Fill_Bottom_Center.png",
            Stretch = TextureRect.StretchMode.Scale,
            CanShrink = true,
            MouseFilter = MouseFilterMode.Ignore
        };

        _bottomRightCorner = new TextureRect
        {
            TexturePath = "/Textures/UI/Chat_Border_Fill_Bottom_Corner_R.png",
            Stretch = TextureRect.StretchMode.Scale,
            CanShrink = true,
            MouseFilter = MouseFilterMode.Ignore
        };

        _rightBorder = new TextureRect
        {
            TexturePath = "/Textures/UI/Chat_Border_R.png",
            Stretch = TextureRect.StretchMode.Scale,
            CanShrink = true,
            MouseFilter = MouseFilterMode.Ignore
        };

        _titlePanel = new TextureRect
        {
            TexturePath = "/Textures/UI/Chat_Title_Panel.png",
            Stretch = TextureRect.StretchMode.Scale,
            CanShrink = true,
            MouseFilter = MouseFilterMode.Ignore
        };

        _resizeHandle = new ResizeHandle(this);

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
        AddChild(_leftBorder);
        AddChild(_rightBorder);
        AddChild(_bottomLeftCorner);
        AddChild(_bottomCenter);
        AddChild(_bottomRightCorner);
        AddChild(_topLeftCorner);
        AddChild(_topCenter);
        AddChild(_topRightCorner);
        AddChild(_titlePanel);
        AddChild(_resizeHandle);
        AddChild(_collapseButton);
    }

    private void ToggleCollapsed()
    {
        _collapsed = !_collapsed;

        _background.Visible = !_collapsed;
        _leftBorder.Visible = !_collapsed;
        _rightBorder.Visible = !_collapsed;
        _bottomLeftCorner.Visible = !_collapsed;
        _bottomCenter.Visible = !_collapsed;
        _bottomRightCorner.Visible = !_collapsed;
        _topLeftCorner.Visible = !_collapsed;
        _topCenter.Visible = !_collapsed;
        _topRightCorner.Visible = !_collapsed;
        _titlePanel.Visible = !_collapsed;
        _resizeHandle.Visible = !_collapsed;

        InvalidateArrange();
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        foreach (var child in Children)
        {
            child.Measure(availableSize);
        }

        return Vector2.Zero;
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var panelHeight = MathF.Round(MathF.Max(0f, finalSize.Y - VerticalMargin * 2f));
        var basePanelWidth = MathF.Round(panelHeight * EquipmentAspect);
        var expandedPanelWidth = MathF.Round(basePanelWidth * _widthMultiplier);
        var panelWidth = _collapsed ? 0f : expandedPanelWidth;
        var panelTop = MathF.Round((finalSize.Y - panelHeight) * 0.5f);
        var panelLeft = MathF.Round(finalSize.X - panelWidth);
        var panelRight = panelLeft + panelWidth;
        PanelBounds = UIBox2.FromDimensions(new Vector2(panelLeft, panelTop), new Vector2(panelWidth, panelHeight));
        var borderWidth = MathF.Round(panelHeight * BorderAspect);
        var edgeHeight = MathF.Round(panelHeight * (EdgeSourceHeight / BorderSourceHeight));
        var edgeCornerWidth = MathF.Round(edgeHeight * EdgeCornerAspect);
        var edgeCenterOverlap = MathF.Round(edgeHeight * (EdgeCenterOverlapSourceWidth / EdgeSourceHeight));
        var titleWidth = MathF.Round(basePanelWidth);
        var titleHeight = MathF.Round(titleWidth / TitleAspect);
        var titleLeft = MathF.Round(panelLeft + (panelWidth - titleWidth) * 0.5f);

        _background.Arrange(UIBox2.FromDimensions(
            new Vector2(panelLeft, panelTop),
            new Vector2(panelWidth, panelHeight)));

        _leftBorder.Arrange(UIBox2.FromDimensions(
            new Vector2(panelLeft, panelTop),
            new Vector2(borderWidth, panelHeight)));

        _rightBorder.Arrange(UIBox2.FromDimensions(
            new Vector2(panelRight - borderWidth, panelTop),
            new Vector2(borderWidth, panelHeight)));

        _bottomLeftCorner.Arrange(UIBox2.FromDimensions(
            new Vector2(panelLeft, panelTop + panelHeight - edgeHeight),
            new Vector2(edgeCornerWidth, edgeHeight)));

        _bottomCenter.Arrange(UIBox2.FromDimensions(
            new Vector2(panelLeft + edgeCornerWidth - edgeCenterOverlap, panelTop + panelHeight - edgeHeight),
            new Vector2(MathF.Max(0f, panelWidth - edgeCornerWidth * 2f + edgeCenterOverlap * 2f), edgeHeight)));

        _bottomRightCorner.Arrange(UIBox2.FromDimensions(
            new Vector2(panelRight - edgeCornerWidth, panelTop + panelHeight - edgeHeight),
            new Vector2(edgeCornerWidth, edgeHeight)));

        _topLeftCorner.Arrange(UIBox2.FromDimensions(
            new Vector2(panelLeft, panelTop),
            new Vector2(edgeCornerWidth, edgeHeight)));

        _topCenter.Arrange(UIBox2.FromDimensions(
            new Vector2(panelLeft + edgeCornerWidth - edgeCenterOverlap, panelTop),
            new Vector2(MathF.Max(0f, panelWidth - edgeCornerWidth * 2f + edgeCenterOverlap * 2f), edgeHeight)));

        _topRightCorner.Arrange(UIBox2.FromDimensions(
            new Vector2(panelRight - edgeCornerWidth, panelTop),
            new Vector2(edgeCornerWidth, edgeHeight)));

        _titlePanel.Arrange(UIBox2.FromDimensions(
            new Vector2(titleLeft, panelTop),
            new Vector2(titleWidth, titleHeight)));

        _resizeHandle.Arrange(UIBox2.FromDimensions(
            new Vector2(panelLeft, panelTop),
            new Vector2(ResizeHandleWidth, panelHeight)));

        var buttonX = _collapsed
            ? MathF.Max(0f, finalSize.X - CollapseButtonSize.X)
            : MathF.Max(0f, panelLeft - CollapseButtonSize.X * 0.5f);
        var buttonY = (finalSize.Y - CollapseButtonSize.Y) * 0.5f;
        _collapseButton.Arrange(UIBox2.FromDimensions(
            new Vector2(buttonX, buttonY),
            CollapseButtonSize));

        var contentLeft = panelLeft + borderWidth + ContentPadding;
        var contentTop = panelTop + titleHeight + ContentPadding;
        var contentSize = new Vector2(
            MathF.Max(0f, panelWidth - borderWidth * 2f - ContentPadding * 2f),
            MathF.Max(0f, panelHeight - titleHeight - edgeHeight - ContentPadding * 2f));

        foreach (var child in Children)
        {
            if (child == _background
                || child == _leftBorder
                || child == _rightBorder
                || child == _bottomLeftCorner
                || child == _bottomCenter
                || child == _bottomRightCorner
                || child == _topLeftCorner
                || child == _topCenter
                || child == _topRightCorner
                || child == _titlePanel
                || child == _resizeHandle
                || child == _collapseButton)
                continue;

            child.Visible = !_collapsed;
            child.Arrange(UIBox2.FromDimensions(new Vector2(contentLeft, contentTop), contentSize));
        }

        return finalSize;
    }

    private void ResizeFromMouse(float mouseX)
    {
        var panelHeight = MathF.Max(0f, Size.Y - VerticalMargin * 2f);
        var basePanelWidth = panelHeight * EquipmentAspect;

        if (basePanelWidth <= 0f)
            return;

        var desiredWidth = Math.Clamp(Size.X - mouseX, basePanelWidth, basePanelWidth * MaxWidthMultiplier);
        _widthMultiplier = desiredWidth / basePanelWidth;
        InvalidateArrange();
    }

    private sealed class ResizeHandle : Control
    {
        private readonly MythosChatPanel _owner;
        private bool _dragging;

        public ResizeHandle(MythosChatPanel owner)
        {
            _owner = owner;
            MouseFilter = MouseFilterMode.Stop;
            CanKeyboardFocus = true;
            KeyboardFocusOnClick = true;
            DefaultCursorShape = CursorShape.HResize;
        }

        protected override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            if (args.Function == EngineKeyFunctions.UIClick)
            {
                _dragging = true;
                UserInterfaceManager.ControlFocused = this;
                args.Handle();
            }

            base.KeyBindDown(args);
        }

        protected override void KeyBindUp(GUIBoundKeyEventArgs args)
        {
            if (args.Function == EngineKeyFunctions.UIClick && _dragging)
            {
                _dragging = false;
                ReleaseKeyboardFocus();
                args.Handle();
            }

            base.KeyBindUp(args);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (!_dragging)
                return;

            _owner.ResizeFromMouse(UserInterfaceManager.MousePositionScaled.Position.X);
        }
    }

    private sealed class TiledBackground : Control
    {
        private const string TexturePath = "/Textures/UI/Chat_BG.png";
        private const float TileSize = 320f;
        private static readonly Color BackgroundColor = new(0.85f, 0.85f, 0.85f);

        private readonly Texture _texture;

        public TiledBackground()
        {
            _texture = IoCManager.Resolve<IResourceCache>().GetTexture(TexturePath);
            MouseFilter = MouseFilterMode.Ignore;
            RectClipContent = true;
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            if (PixelSize.X <= 0f || PixelSize.Y <= 0f)
                return;

            for (var y = 0f; y < PixelSize.Y; y += TileSize)
            {
                for (var x = 0f; x < PixelSize.X; x += TileSize)
                {
                    handle.DrawTextureRect(
                        _texture,
                        UIBox2.FromDimensions(new Vector2(x, y), new Vector2(TileSize, TileSize)),
                        BackgroundColor);
                }
            }
        }
    }
}

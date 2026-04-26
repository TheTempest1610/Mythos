using System.Numerics;
using Content.Client.Stylesheets;
using Content.Client.Mythos.UserInterface.OldHud;
using Content.Shared.Mythos.Magic.Mana;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Maths;

namespace Content.Client.Mythos.UserInterface.ManaHud;

/// <summary>
/// Screen-space HUD overlay rendering the local player's mana as a small
/// labelled bar. Drawn directly over the HUD rather than composed through
/// the XAML game screen so the Mythos fork keeps its zero-upstream-edit
/// invariant. Adding a control to <c>DefaultGameScreen.xaml</c> would be
/// the idiomatic path but costs an upstream touch-point.
///
/// Polls <see cref="ManaComponent"/> and <see cref="SharedManaSystem.CalculateEffectiveMana"/>
/// every frame so the displayed value tracks regen lazily without
/// requiring per-tick network dirty updates.
/// </summary>
public sealed class ManaHudOverlay : Overlay
{
    // Bottom-right anchor, lifted clear of the stock hands/inventory row.
    // Earlier top-anchored positions kept colliding with chat / action bar
    // panels; right-bottom is the most reliably-empty corner of the SS14
    // default HUD. The queue widget stacks directly above.
    private const float RightMargin = 24f;
    private const float BottomMargin = 260f;
    private static readonly Vector2 BarSize = new(140f, 16f);

    private static readonly Color FrameColor = Color.FromHex("#1b0d2a");
    private static readonly Color EmptyColor = Color.FromHex("#2a1e3d");
    private static readonly Color FillColor = Color.FromHex("#6b3ff5");
    private static readonly Color TextColor = Color.White;

    private readonly IEntityManager _entMan;
    private readonly IPlayerManager _player;
    private readonly IUserInterfaceManager _ui;
    private readonly Font _font;
    private readonly OldHudVisibilityUIController _oldHud;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public ManaHudOverlay(
        IEntityManager entMan,
        IPlayerManager player,
        IUserInterfaceManager ui,
        IResourceCache resourceCache,
        OldHudVisibilityUIController oldHud)
    {
        _entMan = entMan;
        _player = player;
        _ui = ui;
        _font = resourceCache.NotoStack();
        _oldHud = oldHud;
        // Draw above the chat and other HUD panels so the mana readout
        // stays legible regardless of what's been opened on top of it.
        ZIndex = 500;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_oldHud.IsOldHudHidden)
            return;

        if (_player.LocalEntity is not { } local)
            return;

        if (!_entMan.TryGetComponent<ManaComponent>(local, out var mana))
            return;

        var timing = _entMan.System<SharedManaSystem>();
        var effective = timing.GetEffectiveMana(local, mana);
        var fraction = mana.Max <= 0f ? 0f : Math.Clamp(effective / mana.Max, 0f, 1f);

        var uiScale = _ui.RootControl.UIScale;
        var barSize = BarSize * uiScale;
        var viewportSize = args.ViewportBounds.Size;
        var origin = new Vector2(
            viewportSize.X - barSize.X - RightMargin * uiScale,
            viewportSize.Y - barSize.Y - BottomMargin * uiScale);

        // Outer frame + empty background.
        var frameRect = new UIBox2(origin, origin + barSize);
        args.ScreenHandle.DrawRect(frameRect, FrameColor);

        var innerInset = 2f * uiScale;
        var innerRect = new UIBox2(
            origin + new Vector2(innerInset, innerInset),
            origin + barSize - new Vector2(innerInset, innerInset));
        args.ScreenHandle.DrawRect(innerRect, EmptyColor);

        // Filled portion, left-to-right.
        if (fraction > 0f)
        {
            var fillRect = new UIBox2(
                innerRect.Left,
                innerRect.Top,
                innerRect.Left + (innerRect.Right - innerRect.Left) * fraction,
                innerRect.Bottom);
            args.ScreenHandle.DrawRect(fillRect, FillColor);
        }

        // Numeric readout centred-ish over the bar.
        var label = $"MP {(int)MathF.Round(effective)} / {(int)MathF.Round(mana.Max)}";
        var textOrigin = origin + new Vector2(6f * uiScale, -16f * uiScale);
        args.ScreenHandle.DrawString(_font, textOrigin, label, uiScale, TextColor);
    }
}

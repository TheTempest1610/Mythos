using System.Numerics;
using Content.Client.Stylesheets;
using Content.Shared.Mythos.Combat.Queue;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Maths;

namespace Content.Client.Mythos.UserInterface.QueueHud;

/// <summary>
/// Screen-space overlay that renders the local player's combat queue as a
/// row of coloured slot tiles directly below the mana bar. Each tile
/// carries a single-letter kind tag and a subtle highlight for the active
/// (currently casting) slot, so players can see at a glance what's queued
/// up and which entry is being channelled.
///
/// Follows the same no-upstream-edit discipline as <c>ManaHudOverlay</c>.
/// A proper XAML widget would be tidier but would cost the first touch
/// to <c>DefaultGameScreen.xaml</c>.
/// </summary>
public sealed class CombatQueueHudOverlay : Overlay
{
    // Right-edge anchor, positioned BELOW the mana bar. Mana bar bottom is
    // 260 px above the viewport bottom; queue row top at 244 px above the
    // bottom (BottomMargin=216 + slotHeight=28) leaves a 16 px gap between
    // the bar's bottom edge and the queue's top edge. 216 px from the
    // bottom still clears the stock hands/hotbar row underneath.
    private const float RightMargin = 24f;
    private const float BottomMargin = 216f;
    private static readonly Vector2 SlotSize = new(28f, 28f);
    private const float SlotSpacing = 4f;

    private static readonly Color FrameColor = Color.FromHex("#1b0d2a");
    private static readonly Color EmptySlotColor = Color.FromHex("#2a1e3d");
    private static readonly Color ActiveHighlight = Color.FromHex("#f5c842");
    private static readonly Color TextColor = Color.White;

    private readonly IEntityManager _entMan;
    private readonly IPlayerManager _player;
    private readonly IUserInterfaceManager _ui;
    private readonly Font _font;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public CombatQueueHudOverlay(
        IEntityManager entMan,
        IPlayerManager player,
        IUserInterfaceManager ui,
        IResourceCache resourceCache)
    {
        _entMan = entMan;
        _player = player;
        _ui = ui;
        _font = resourceCache.NotoStack(variation: "Bold", size: 12);
        // Match the mana bar's ZIndex so it stays above chat and other HUD.
        ZIndex = 500;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_player.LocalEntity is not { } local)
            return;

        if (!_entMan.TryGetComponent<CombatQueueComponent>(local, out var queue))
            return;

        if (queue.Queue.Count == 0)
            return;

        var uiScale = _ui.RootControl.UIScale;
        var slotSize = SlotSize * uiScale;
        var spacing = SlotSpacing * uiScale;
        var viewportSize = args.ViewportBounds.Size;

        // Right-align the row: the rightmost slot (head of the queue) sits
        // flush with the mana bar's right edge; subsequent slots extend
        // leftward, preserving FIFO-read-from-the-right.
        var rowWidth = (slotSize.X + spacing) * queue.Queue.Count - spacing;
        var rowRightEdge = viewportSize.X - RightMargin * uiScale;
        var rowLeftEdge = rowRightEdge - rowWidth;
        var rowTop = viewportSize.Y - slotSize.Y - BottomMargin * uiScale;

        for (var i = 0; i < queue.Queue.Count; i++)
        {
            var slot = queue.Queue[i];
            var slotLeft = rowLeftEdge + i * (slotSize.X + spacing);
            var slotRect = new UIBox2(
                new Vector2(slotLeft, rowTop),
                new Vector2(slotLeft + slotSize.X, rowTop + slotSize.Y));

            // Outer frame.
            args.ScreenHandle.DrawRect(slotRect, FrameColor);

            // Inner fill with kind-coloured tint + active-slot highlight.
            var inset = 2f * uiScale;
            var innerRect = new UIBox2(
                slotRect.Left + inset, slotRect.Top + inset,
                slotRect.Right - inset, slotRect.Bottom - inset);

            var kindColor = ColorFor(slot.Kind);
            var isActive = queue.CastingSlot == slot.Slot;
            args.ScreenHandle.DrawRect(innerRect, isActive ? ActiveHighlight : EmptySlotColor);

            // Kind tag: a small solid block at the top + a single letter in
            // the middle is the lightest-weight identifier we can put on
            // a 28 px tile without icons.
            var tagHeight = 6f * uiScale;
            var tagRect = new UIBox2(
                innerRect.Left, innerRect.Top,
                innerRect.Right, innerRect.Top + tagHeight);
            args.ScreenHandle.DrawRect(tagRect, kindColor);

            var letter = LetterFor(slot.Kind);
            var letterOrigin = new Vector2(
                slotRect.Left + (slotSize.X - 7f * uiScale) / 2f,
                slotRect.Top + slotSize.Y * 0.35f);
            args.ScreenHandle.DrawString(_font, letterOrigin, letter, uiScale, TextColor);
        }
    }

    private static Color ColorFor(QueuedActionKind kind) => kind switch
    {
        QueuedActionKind.HeavyAttack => Color.FromHex("#b0b0b0"),
        QueuedActionKind.MagicMissile => Color.FromHex("#6b3ff5"),
        QueuedActionKind.Fireball => Color.FromHex("#f56b2a"),
        _ => Color.White,
    };

    private static string LetterFor(QueuedActionKind kind) => kind switch
    {
        QueuedActionKind.HeavyAttack => "H",
        QueuedActionKind.MagicMissile => "M",
        QueuedActionKind.Fireball => "F",
        _ => "?",
    };
}

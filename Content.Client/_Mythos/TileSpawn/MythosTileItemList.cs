using System;
using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Maths;

namespace Content.Client._Mythos.TileSpawn;

/// <summary>
/// Mythos: extension of <see cref="ItemList"/> that detects which variant cell of a
/// tile's icon strip the user clicked, and draws a yellow outline around the chosen
/// cell. Lets the tile spawn panel select tile + variant in a single click.
///
/// Each item carries a per-row variant count, supplied via <see cref="AddTileItem"/>.
/// </summary>
public sealed class MythosTileItemList : ItemList
{
    private const int CellPx = 32;

    private readonly List<int> _variantCounts = new();

    /// <summary>Fires on click. Arguments are (item index, chosen variant index).</summary>
    public event Action<int, byte>? OnItemVariantSelected;

    public int? SelectedItemIndex { get; private set; }
    public byte SelectedVariant { get; private set; }

    public void AddTileItem(string text, Texture? icon, int variants)
    {
        AddItem(text, icon);
        _variantCounts.Add(variants < 1 ? 1 : variants);
    }

    public void ClearTileItems()
    {
        Clear();
        _variantCounts.Clear();
        SelectedItemIndex = null;
        SelectedVariant = 0;
    }

    /// <summary>
    /// Mirror what <see cref="ItemList.Draw"/> picks per-item: selected, disabled,
    /// or default item background. The stylebox's content margin is what determines
    /// where the icon actually starts, so click math and highlight drawing both need it.
    /// </summary>
    private Robust.Client.Graphics.StyleBox PickItemBackground(Item item)
    {
        if (item.Disabled)
            return ActualDisabledItemBackground;
        if (item.Selected)
            return ActualSelectedItemBackground;
        return ActualItemBackground;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        if (args.Function == EngineKeyFunctions.UIClick)
        {
            for (var idx = 0; idx < Count; idx++)
            {
                var item = this[idx];
                if (item.Region is not { } region)
                    continue;
                if (!region.Contains(args.RelativePixelPosition))
                    continue;

                var variantCount = idx < _variantCounts.Count ? _variantCounts[idx] : 1;
                byte variant = 0;

                if (item.Icon != null && variantCount > 1)
                {
                    var contentBox = PickItemBackground(item).GetContentBox(region, UIScale);
                    var cellWidth = CellPx * item.IconScale;
                    if (cellWidth > 0)
                    {
                        var localX = args.RelativePixelPosition.X - contentBox.Left;
                        var v = (int)(localX / cellWidth);
                        if (v < 0) v = 0;
                        if (v > variantCount - 1) v = variantCount - 1;
                        variant = (byte)v;
                    }
                }

                SelectedItemIndex = idx;
                SelectedVariant = variant;
                OnItemVariantSelected?.Invoke(idx, variant);
                break;
            }
        }

        base.KeyBindDown(args);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (SelectedItemIndex is not { } selIdx || selIdx >= Count)
            return;

        var item = this[selIdx];
        if (item.Region is not { } region || item.Icon == null)
            return;

        var contentBox = PickItemBackground(item).GetContentBox(region, UIScale);
        var cellSize = CellPx * item.IconScale;
        var x0 = contentBox.Left + SelectedVariant * cellSize;
        var y0 = contentBox.Top;
        var box = UIBox2.FromDimensions(x0, y0, cellSize, cellSize);

        // Draw a 2-pixel highlight by drawing two outlines at slight offsets.
        handle.DrawRect(box, Color.Yellow, filled: false);
        var outerBox = new UIBox2(box.Left - 1, box.Top - 1, box.Right + 1, box.Bottom + 1);
        handle.DrawRect(outerBox, Color.Yellow.WithAlpha(0.5f), filled: false);
    }
}

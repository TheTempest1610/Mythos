using Content.Client.Humanoid;
using Content.Shared.Body;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Client.Humanoid;

// Mythos: per-category color memory and tinting hook for the Features
// chargen tab. The Mythos chargen UI exposes one set of color sliders per
// OV chargen category (Hair, Ears, Tail, ...) instead of per-feature, so
// switching from one feature to another within a category retains the
// player's chosen colors and live-tints every preview thumbnail in that
// category.
//
// This partial lives in Content.Client/_Mythos/ to keep the addition
// containerised; only the `sealed class -> sealed partial class` flip in
// the upstream MarkingsViewModel.cs is shared.
public sealed partial class MarkingsViewModel
{
    private readonly Dictionary<string, List<Color>> _categoryColors = new();

    /// <summary>
    /// Raised when a category's stored colors change. Subscribers should
    /// re-tint their thumbnails / refresh their swatches.
    /// </summary>
    public event Action<string>? CategoryColorsChanged;

    /// <summary>
    /// Returns the current per-slot colors stored for a category, or null
    /// if no colors have been picked yet (caller should fall back to a
    /// default — usually the marking's coloring strategy).
    /// </summary>
    public IReadOnlyList<Color>? GetCategoryColors(string category)
    {
        if (_categoryColors.TryGetValue(category, out var colors))
            return colors;
        return null;
    }

    /// <summary>
    /// Set a category's color for one slot. Auto-grows the slot list and
    /// fills missing slots with white. Fires <see cref="CategoryColorsChanged" />
    /// so visible pickers refresh their previews.
    /// </summary>
    public void SetCategoryColor(string category, int slotIndex, Color color)
    {
        if (!_categoryColors.TryGetValue(category, out var colors))
        {
            colors = new List<Color>();
            _categoryColors[category] = colors;
        }
        while (colors.Count <= slotIndex)
            colors.Add(Color.White);
        colors[slotIndex] = color;
        CategoryColorsChanged?.Invoke(category);
    }

    /// <summary>
    /// Seed a category's colors from a marking's current state if no
    /// colors have been set yet for that category. Lets the picker
    /// inherit "the colors I had on the previously-selected feature in
    /// this category" instead of resetting to white when the user opens
    /// the tab.
    /// </summary>
    public void SeedCategoryColors(string category, IReadOnlyList<Color> source)
    {
        if (_categoryColors.ContainsKey(category))
            return;
        _categoryColors[category] = new List<Color>(source);
    }

    /// <summary>
    /// When the player selects a new marking in a category, propagate the
    /// stored category colors onto it so the live-rendered character
    /// inherits the per-category palette without the player having to
    /// re-tint every time they swap features.
    /// </summary>
    public void ApplyCategoryColorsToMarking(
        string category,
        ProtoId<OrganCategoryPrototype> organ,
        Content.Shared.Humanoid.HumanoidVisualLayers layer,
        ProtoId<MarkingPrototype> markingId)
    {
        if (!_categoryColors.TryGetValue(category, out var colors))
            return;
        for (var i = 0; i < colors.Count; i++)
            TrySetMarkingColor(organ, layer, markingId, i, colors[i]);
    }
}

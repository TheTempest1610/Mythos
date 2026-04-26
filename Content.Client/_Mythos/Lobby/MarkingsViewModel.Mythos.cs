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

    /// <summary>
    /// Apply every piece of synced category state (colors, toggles,
    /// size, variant) to the named marking. Called on selection so a
    /// freshly-picked feature inherits the player's chosen palette,
    /// open/closed wing pose, current size slider, and current variant
    /// dropdown without them having to re-set everything.
    /// </summary>
    public void ApplyCategoryStateToMarking(
        string category,
        ProtoId<OrganCategoryPrototype> organ,
        Content.Shared.Humanoid.HumanoidVisualLayers layer,
        ProtoId<MarkingPrototype> markingId)
    {
        ApplyCategoryColorsToMarking(category, organ, layer, markingId);

        if (_categoryBools.TryGetValue(category, out var toggles))
            foreach (var (k, v) in toggles)
                TrySetMarkingMythosToggle(organ, layer, markingId, k, v);

        if (_categorySize.TryGetValue(category, out var size))
            TrySetMarkingMythosSize(organ, layer, markingId, size);

        if (_categoryVariant.TryGetValue(category, out var variant))
            TrySetMarkingMythosVariant(organ, layer, markingId, variant);
    }

    // --- Per-category bool dimensions (toggles like is_open, lactating).
    private readonly Dictionary<string, Dictionary<string, bool>> _categoryBools = new();

    /// <summary>
    /// Raised when a category's stored toggle state changes. Subscribers
    /// re-render any selected markings + refresh the toggle button UI.
    /// </summary>
    public event Action<string>? CategoryTogglesChanged;

    public IReadOnlyDictionary<string, bool>? GetCategoryToggles(string category)
        => _categoryBools.TryGetValue(category, out var t) ? t : null;

    public bool GetCategoryToggle(string category, string name)
        => _categoryBools.TryGetValue(category, out var t)
           && t.TryGetValue(name, out var v) && v;

    public void SetCategoryToggle(string category, string name, bool value)
    {
        if (!_categoryBools.TryGetValue(category, out var t))
        {
            t = new Dictionary<string, bool>();
            _categoryBools[category] = t;
        }
        if (t.TryGetValue(name, out var existing) && existing == value)
            return;
        t[name] = value;
        CategoryTogglesChanged?.Invoke(category);
    }

    // --- Per-category int dimension (size slider, e.g. penis_size 1-5).
    private readonly Dictionary<string, int> _categorySize = new();

    /// <summary>
    /// Raised when a category's stored size index changes. Subscribers
    /// re-render any selected markings + refresh the slider UI.
    /// </summary>
    public event Action<string>? CategorySizeChanged;

    public int GetCategorySize(string category)
        => _categorySize.TryGetValue(category, out var s) ? s : 0;

    public void SetCategorySize(string category, int value)
    {
        if (_categorySize.TryGetValue(category, out var existing) && existing == value)
            return;
        _categorySize[category] = value;
        CategorySizeChanged?.Invoke(category);
    }

    // --- Apply category state to live marking instances. The renderer
    // reads Marking.MythosToggles / Marking.MythosSizeIndex directly, so
    // we have to push the category memory onto the in-list instance by
    // replacing it with a `with`-mutated copy (Marking is a record struct).

    public void TrySetMarkingMythosToggle(
        Robust.Shared.Prototypes.ProtoId<Content.Shared.Body.OrganCategoryPrototype> organ,
        Content.Shared.Humanoid.HumanoidVisualLayers layer,
        Robust.Shared.Prototypes.ProtoId<MarkingPrototype> markingId,
        string toggleName,
        bool value)
    {
        if (!_markings.TryGetValue(organ, out var markingSet))
            return;
        if (!markingSet.TryGetValue(layer, out var markings))
            return;
        var idx = markings.FindIndex(m => m.MarkingId == markingId);
        if (idx == -1)
            return;
        markings[idx] = markings[idx].WithMythosToggle(toggleName, value);
        MarkingsChanged?.Invoke(organ, layer);
    }

    public void TrySetMarkingMythosSize(
        Robust.Shared.Prototypes.ProtoId<Content.Shared.Body.OrganCategoryPrototype> organ,
        Content.Shared.Humanoid.HumanoidVisualLayers layer,
        Robust.Shared.Prototypes.ProtoId<MarkingPrototype> markingId,
        int size)
    {
        if (!_markings.TryGetValue(organ, out var markingSet))
            return;
        if (!markingSet.TryGetValue(layer, out var markings))
            return;
        var idx = markings.FindIndex(m => m.MarkingId == markingId);
        if (idx == -1)
            return;
        markings[idx] = markings[idx].WithMythosSize(size);
        MarkingsChanged?.Invoke(organ, layer);
    }

    // --- Per-category variant dimension (Penis silhouette dropdown,
    // Breasts arrangement dropdown). Synced like color and size: every
    // selected marking in the category gets the same variant applied,
    // and switching to a sibling item inherits the stored variant.

    private readonly Dictionary<string, string> _categoryVariant = new();

    public event Action<string>? CategoryVariantChanged;

    public string? GetCategoryVariant(string category)
        => _categoryVariant.TryGetValue(category, out var v) ? v : null;

    public void SetCategoryVariant(string category, string variant)
    {
        if (_categoryVariant.TryGetValue(category, out var existing) && existing == variant)
            return;
        _categoryVariant[category] = variant;
        CategoryVariantChanged?.Invoke(category);
    }

    public void TrySetMarkingMythosVariant(
        Robust.Shared.Prototypes.ProtoId<Content.Shared.Body.OrganCategoryPrototype> organ,
        Content.Shared.Humanoid.HumanoidVisualLayers layer,
        Robust.Shared.Prototypes.ProtoId<MarkingPrototype> markingId,
        string variant)
    {
        if (!_markings.TryGetValue(organ, out var markingSet))
            return;
        if (!markingSet.TryGetValue(layer, out var markings))
            return;
        var idx = markings.FindIndex(m => m.MarkingId == markingId);
        if (idx == -1)
            return;
        markings[idx] = markings[idx].WithMythosVariant(variant);
        MarkingsChanged?.Invoke(organ, layer);
    }
}

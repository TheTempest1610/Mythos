namespace Content.Shared.Humanoid.Markings;

// Mythos: extensions to upstream MarkingPrototype.
//
// Upstream SS14 calls every appearance-customization unit a "marking"
// regardless of its OV semantics. Mythos uses this single type for
// several conceptually distinct things, distinguished at runtime by the
// Category field on the prototype:
//
//   * OV sprite_accessory ("features"): hair, facial hair, ears, snouts,
//     tails, horns, antennas, frills, wings, accessories, face details,
//     eyes, genitals, etc. Imported under
//     Resources/Prototypes/_Mythos/Chargen/Features/. Surfaced in the
//     Mythos chargen "Features" tab via MythosFeaturePicker.
//
//   * OV body_marking (real "markings"): chest stripes, sock patterns,
//     tiger markings, gradient overlays, etc. Future Phase 2B port. Will
//     live under Resources/Prototypes/_Mythos/Chargen/BodyMarkings/ and
//     surface in the existing "Markings" chargen tab.
//
//   * Vanilla SS14 markings (slimes, vox, lizard markings, etc.) — left
//     unchanged. They get no Category value, so FeatureCategory() falls
//     through to the BodyPart enum string and the picker still groups
//     them the way upstream expects.
//
// The fields below add OV-specific layering / coloring affordances:
//
//   SpriteBodyParts:    per-sprite render-layer override. Lets one
//                       prototype paint pixels at multiple humanoid
//                       layers (e.g. tail _behind state on BodyBehind,
//                       _front state on Tail).
//   SpriteColorIndices: per-sprite color-slot index. Lets paired
//                       BEHIND/FRONT sprites of the same OV color slot
//                       share a single picker color so a 2-color tail
//                       stays a 2-color picker even with 4 sprites.
//   Category:           OV chargen category name, drives picker tab
//                       grouping (separate from render layer).
//
// All three are optional; absent means "behave like upstream."
public sealed partial class MarkingPrototype
{
    [DataField]
    public List<HumanoidVisualLayers?>? SpriteBodyParts;

    /// <summary>
    /// Mythos: OV chargen category this prototype belongs to. Used to
    /// group prototypes into chargen tabs and to disambiguate the
    /// double-duty role of upstream <see cref="MarkingPrototype"/>:
    /// <list type="bullet">
    ///   <item><c>"Hair"</c>, <c>"Ears"</c>, <c>"Tail"</c>, <c>"Snout"</c>...
    ///         — OV sprite_accessory ("features"), shown in the
    ///         Features tab via <c>MythosFeaturePicker</c>.</item>
    ///   <item><c>"BodyMarking"</c> (Phase 2B) — OV body_marking patterns
    ///         (stripes, socks, tiger), shown in the Markings tab.</item>
    ///   <item><c>null</c> — vanilla SS14 markings or anything that
    ///         hasn't opted in. <see cref="FeatureCategory"/> falls back
    ///         to the BodyPart enum string and upstream pickers continue
    ///         to group them by render layer the way they always did.</item>
    /// </list>
    /// Independent of <see cref="MarkingPrototype.BodyPart"/>: BodyPart
    /// drives render z-order (where pixels paint), Category drives
    /// chargen UI grouping (which tab it lives in). Several categories
    /// can share one BodyPart — e.g. Ears, Horns, and Antennas all sit
    /// at HeadTop but get distinct chargen tabs.
    /// </summary>
    [DataField]
    public string? Category;

    public string FeatureCategory() =>
        string.IsNullOrEmpty(Category) ? BodyPart.ToString() : Category;

    /// <summary>
    /// Mythos: optional positional list mapping each entry in <see cref="Sprites" />
    /// to a color-slot index. When set, the picker presents only
    /// <c>max(SpriteColorIndices) + 1</c> color pickers; multiple sprites
    /// sharing an index get the same color. Lets paired BEHIND/FRONT
    /// sprites of the same OV color slot stay in sync without doubling the
    /// picker UI (a fox tail with 2 colors stays a 2-color picker even
    /// though the marking has 4 sprites).
    /// </summary>
    [DataField]
    public List<int>? SpriteColorIndices;

    public HumanoidVisualLayers GetSpriteBodyPart(int index)
    {
        if (SpriteBodyParts is { } overrides
            && index < overrides.Count
            && overrides[index] is { } overridden)
        {
            return overridden;
        }
        return BodyPart;
    }

    public int GetSpriteColorIndex(int spriteIndex)
    {
        if (SpriteColorIndices is { } indices && spriteIndex < indices.Count)
            return indices[spriteIndex];
        return spriteIndex;
    }

    public int ColorSlotCount()
    {
        if (SpriteColorIndices is { } indices && indices.Count > 0)
        {
            var max = -1;
            foreach (var i in indices)
                if (i > max) max = i;
            return max + 1;
        }
        return Sprites?.Count ?? 0;
    }
}

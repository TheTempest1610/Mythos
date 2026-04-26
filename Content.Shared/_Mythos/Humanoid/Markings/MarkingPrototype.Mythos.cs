using Robust.Shared.Utility;

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

    /// <summary>
    /// Mythos: per-category bool dimensions this prototype responds to
    /// (e.g. <c>"is_open"</c> for wings, <c>"functional"</c> for penis,
    /// <c>"lactating"</c> for breasts). Each name corresponds to a key in
    /// <see cref="MythosToggleStates"/> that gives the alternate sprite
    /// list when the named toggle is true. The chargen UI shows one
    /// toggle button per name on the selected feature item, with state
    /// synced across the category. Empty / null means no toggles.
    /// </summary>
    [DataField]
    public List<string>? MythosToggles;

    /// <summary>
    /// Mythos: per-toggle-name alternate sprite list. When the named
    /// toggle is true, the renderer uses this sprite list instead of
    /// <see cref="Sprites"/> for the affected layers. The list shape
    /// must match Sprites (same length, same per-index color slot
    /// alignment).
    /// </summary>
    [DataField]
    public Dictionary<string, List<SpriteSpecifier>>? MythosToggleStates;

    /// <summary>
    /// Mythos: synced size dimension. When set, the chargen UI shows a
    /// shared size slider for the category that picks one of these
    /// sprite lists by index (clamped to <c>[0, Count-1]</c>). The
    /// renderer uses the indexed list instead of <see cref="Sprites"/>.
    /// Used for OV's continuous-size organs (penis_size 1-5,
    /// breast_size 1-5). Empty / null means no size slider.
    /// </summary>
    [DataField]
    public List<List<SpriteSpecifier>>? MythosSizeStates;

    /// <summary>
    /// Resolves the active sprite list for this prototype given the
    /// current category state. Bool toggles short-circuit size states:
    /// if any toggle in <see cref="MythosToggles"/> is true and has a
    /// matching key in <see cref="MythosToggleStates"/>, that list wins
    /// over both size and the default. Otherwise size wins if set, and
    /// otherwise the default <see cref="Sprites"/>.
    /// </summary>
    /// <summary>
    /// Resolve the active sprite list for this prototype given the
    /// marking's current per-instance state.
    /// Precedence (first match wins):
    ///   1. <see cref="MythosVariants"/> + variant name -> variant sprites
    ///      (for Penis / Breasts where variant is a category-level dropdown).
    ///   2. <see cref="MythosToggles"/> + on toggle -> toggle alternate
    ///      (Wings is_open, etc.).
    ///   3. <see cref="MythosSizeStates"/> + sizeIndex -> size variant.
    ///   4. Default <see cref="Sprites"/>.
    /// </summary>
    public List<SpriteSpecifier> GetActiveSprites(
        IReadOnlyDictionary<string, bool>? toggles,
        int? sizeIndex,
        string? variantName = null)
    {
        if (variantName is not null
            && MythosVariantStates is { } variants
            && variants.TryGetValue(variantName, out var vSprites))
        {
            return vSprites;
        }
        if (MythosToggles is { } names && MythosToggleStates is { } states && toggles is not null)
        {
            foreach (var name in names)
            {
                if (toggles.TryGetValue(name, out var on) && on
                    && states.TryGetValue(name, out var alt))
                {
                    return alt;
                }
            }
        }
        if (sizeIndex is { } i
            && MythosSizeStates is { Count: > 0 } sizes)
        {
            var clamped = System.Math.Clamp(i, 0, sizes.Count - 1);
            return sizes[clamped];
        }
        return Sprites;
    }

    /// <summary>
    /// Maximum size index this prototype supports + 1, or 0 if no size
    /// dimension. Drives the picker's size slider range.
    /// </summary>
    public int MythosSizeCount() => MythosSizeStates?.Count ?? 0;

    /// <summary>
    /// Mythos: synced category-level variant dimension. When set, the
    /// chargen UI shows a shared dropdown for the category that picks
    /// one of these named variants; the renderer uses
    /// <see cref="MythosVariantStates"/>[name] as the active sprite
    /// list. Used by Penis (variant = silhouette: Plain / Knotted /
    /// Equine / ...) and Breasts (variant = arrangement: Pair / Quad /
    /// Sextuple) where the size dimension is the selectable item axis
    /// and the variant axis lives at category level.
    /// </summary>
    [DataField]
    public List<string>? MythosVariants;

    [DataField]
    public Dictionary<string, List<SpriteSpecifier>>? MythosVariantStates;

    /// <summary>
    /// Mythos: explicit picker ordering for prototypes within a category
    /// where alphabetical-by-name doesn't match OV's intended sequence
    /// (e.g., size labels Flat/Small/Medium/Large/Enormous would sort to
    /// Enormous/Flat/Large/Medium/Small alphabetically). Lower values
    /// render first; null falls through to alphabetical.
    /// </summary>
    [DataField]
    public int? MythosOrderIndex;

    /// <summary>
    /// Mythos: when this marking is applied, suppress rendering of any
    /// other applied marking whose Category is "Breasts". Mirrors OV's
    /// covers_breasts flag on /obj/item/undies (bikini, leotard,
    /// athletic_leotard) and the matching is_visible check at
    /// code/modules/mob/dead/new_player/sprite_accessory/genitals.dm:142.
    /// Without this, a chest-covering garment and a breast feature
    /// would both render at their OV-faithful z-layers (UndergarmentBottom
    /// at -43, BodyFrontest at -4), and the breast sprite would
    /// visually overwhelm the garment.
    /// </summary>
    [DataField]
    public bool CoversBreasts;

    /// <summary>
    /// Mythos: when set, the renderer reads the wearer's currently
    /// applied breast marking's <see cref="MythosOrderIndex"/> (which
    /// the breast consolidator stamps with OV's breast_size 0-16) and
    /// uses it as the index into <see cref="MythosSizeStates"/> for
    /// this marking, instead of the marking's own
    /// <see cref="Marking.MythosSizeIndex"/>. Mirrors OV's runtime
    /// state-name synthesis at
    /// code/modules/mob/dead/new_player/sprite_accessory/underwear.dm:36
    /// where the bikini and leotard sprite_accessory rebuild their
    /// icon_state per-render from the wearer's breast organ. Sizes
    /// past the highest declared state are clamped (matches OV's
    /// `breast_size > 5 -> _5`). When the wearer has no breast
    /// marking, the marking's default <see cref="Sprites"/> renders
    /// instead, mirroring OV's `else return "bikini_f_0"` fallback.
    /// </summary>
    [DataField]
    public bool MatchesBreastSize;
}

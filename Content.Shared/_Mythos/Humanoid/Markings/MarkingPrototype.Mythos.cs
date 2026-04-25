namespace Content.Shared.Humanoid.Markings;

// Mythos: per-sprite body-part override for layered OV markings.
//
// Upstream MarkingPrototype renders every sprite in Sprites at the same body
// part layer (BodyPart). OV's tail/ear features encode their z-ordering as
// paired BEHIND/FRONT states whose pixels need to land at different sprite
// layers (one before Chest, one after). SpriteBodyParts is a positional
// list aligned with Sprites: a non-null entry routes that sprite to its
// override layer; a null or missing entry falls back to BodyPart.
public sealed partial class MarkingPrototype
{
    [DataField]
    public List<HumanoidVisualLayers?>? SpriteBodyParts;

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

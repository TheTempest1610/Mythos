using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid.Markings;

/// <summary>
///     Represents a marking ID and its colors
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public partial record struct Marking
{
    /// <summary>
    /// The <see cref="MarkingPrototype"/> referred to by this marking
    /// </summary>
    [DataField(required: true)]
    public ProtoId<MarkingPrototype> MarkingId;

    [DataField("markingColor")]
    private List<Color> _markingColors;

    /// <summary>
    /// The colors taken on by the marking
    /// </summary>
    public IReadOnlyList<Color> MarkingColors => _markingColors;

    /// <summary>
    /// Whether the marking is forced regardless of points
    /// </summary>
    public bool Forced;

    // Mythos: per-instance state for OV-style features that can't be
    // expressed by sprite + colors alone (size slider, toggle bools,
    // variant dropdown). Lives here rather than in a sibling partial
    // because C# (CS0282) forbids splitting struct fields across partial
    // declarations -- field order would be ambiguous and mess up
    // serialization. Helpers (WithMythosSize / WithMythosToggle /
    // WithMythosVariant) live in Marking.Mythos.cs.

    /// <summary>
    /// Mythos: index into <see cref="MarkingPrototype.MythosSizeStates"/>
    /// for sized organs (Penis, Breasts) where size is a synced
    /// category-level slider rather than a separate prototype.
    /// </summary>
    [DataField]
    public int? MythosSizeIndex;

    /// <summary>
    /// Mythos: per-toggle bool state (is_open, functional, lactating,
    /// virile, fertility) keyed by the names in
    /// <see cref="MarkingPrototype.MythosToggles"/>.
    /// </summary>
    [DataField]
    public Dictionary<string, bool>? MythosToggles;

    /// <summary>
    /// Mythos: currently-selected named variant for prototypes that
    /// expose <see cref="MarkingPrototype.MythosVariants"/>. Penis uses
    /// it to flip silhouette (Plain / Knotted / ...) while keeping size
    /// as the picker item; Breasts uses it for arrangement (Pair /
    /// Quad / Sextuple). Null = renderer falls through to the
    /// prototype's default Sprites list.
    /// </summary>
    [DataField]
    public string? MythosVariant;

    public Marking()
    {
        _markingColors = new();
    }

    public Marking(ProtoId<MarkingPrototype> markingId, IEnumerable<Color> colors)
    {
        MarkingId = markingId;
        _markingColors = colors.ToList();
    }

    public Marking(ProtoId<MarkingPrototype> markingId, int colorsCount) : this(markingId,
        Enumerable.Repeat(Color.White, colorsCount).ToList())
    {
    }

    public bool Equals(Marking other)
    {
        return MarkingId.Equals(other.MarkingId)
            && MarkingColors.SequenceEqual(other.MarkingColors)
            && Forced.Equals(other.Forced);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(MarkingId, MarkingColors, Forced);
    }

    public Marking WithColor(Color color) =>
        this with { _markingColors = Enumerable.Repeat(color, MarkingColors.Count).ToList() };

    public Marking WithColorAt(int index, Color color)
    {
        // Mythos: bound-check defensively. The Mythos category-color picker
        // can propagate a color change to every selected marking in a
        // category, and a category may mix prototypes with different color
        // slot counts (e.g., a 1-slot item alongside a 3-slot item). An
        // out-of-range write here used to crash the client; silently no-op
        // instead so the slot count of the marking governs which colors
        // actually apply.
        if (index < 0 || index >= _markingColors.Count)
            return this;
        var newColors = _markingColors.ShallowClone();
        newColors[index] = color;
        return this with { _markingColors = newColors };
    }

    // look this could be better but I don't think serializing
    // colors is the correct thing to do
    //
    // this is still janky imo but serializing a color and feeding
    // it into the default JSON serializer (which is just *fine*)
    // doesn't seem to have compatible interfaces? this 'works'
    // for now but should eventually be improved so that this can,
    // in fact just be serialized through a convenient interface
    public string ToLegacyDbString()
    {
        // reserved character
        string sanitizedName = MarkingId.Id.Replace('@', '_');
        List<string> colorStringList = new();
        foreach (var color in MarkingColors)
            colorStringList.Add(color.ToHex());

        return $"{sanitizedName}@{String.Join(',', colorStringList)}";
    }

    public static Marking? ParseFromDbString(string input)
    {
        if (input.Length == 0) return null;
        var split = input.Split('@');
        if (split.Length != 2) return null;
        List<Color> colorList = new();
        foreach (string color in split[1].Split(','))
        {
            colorList.Add(Color.FromHex(color));
        }

        return new Marking(split[0], colorList);
    }
}

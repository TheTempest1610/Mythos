using Content.Shared.Humanoid;

namespace Content.Shared.Body;

// Mythos: secondary sprite layers driven by the same organ.
//
// Upstream VisualOrganComponent maps a single Sprite + state to one
// HumanoidVisualLayers slot. OV-style legs need a *second* sprite layer
// for the side-view background leg, which has to render BEFORE Chest.
// SecondaryLayers lets a Mythos organ describe additional (layer, state)
// pairs sourced from the same RSI sprite path. The render system applies
// each entry the same way it applies the primary Layer + Data.
public sealed partial class VisualOrganComponent
{
    [DataField]
    public List<MythosSecondaryOrganLayer>? SecondaryLayers;
}

[DataDefinition]
public sealed partial class MythosSecondaryOrganLayer
{
    // Untyped Enum to match upstream VisualOrganComponent.Layer; this lets
    // the YAML use the `enum.HumanoidVisualLayers.X` prefixed form that
    // upstream organs already use.
    [DataField(required: true)]
    public Enum Layer = default!;

    [DataField(required: true)]
    public PrototypeLayerData Data = default!;

    // Mirror of the primary VisualOrganComponent.SexStateOverrides: when the
    // wearer's profile sex matches a key, the layer's RSI state is swapped.
    [DataField]
    public Dictionary<Sex, string>? SexStateOverrides;
}

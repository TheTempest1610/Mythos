using System.Collections.Generic;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Humanoid.Markings;

/// <summary>
/// Mythos-side gradient slot declaration on a <see cref="MarkingPrototype"/>.
///
/// V1.1 plumbing only. The chargen UI does not yet expose gradient pickers
/// and the renderer does not yet blend sprite states by gradient direction.
/// This data class exists so YAML authors can describe gradient intent
/// alongside the existing per-sprite color slots, with V1.2 implementing
/// the picker UI and renderer support without a schema migration.
///
/// Mapped from OV's per-feature gradient pattern: each accessory in OV
/// declares its gradients (typically one or two: "Natural" and "Dye"),
/// and each gradient has a variant list ("None", "Fade Up",
/// "Vertical Split", ...). Selecting a non-None variant pairs the
/// gradient with a chosen secondary color that the renderer eventually
/// blends into the base sprite.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class MythosGradientSlot
{
    /// <summary>
    /// Stable id for the gradient slot. References this entry across
    /// runtime state (e.g. <c>"natural"</c>, <c>"dye"</c>).
    /// </summary>
    [DataField("id", required: true)]
    public string Id { get; private set; } = string.Empty;

    /// <summary>
    /// Locale key for the picker label.
    /// </summary>
    [DataField("displayName", required: true)]
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Allowed variants. The string "None" is reserved as the unset
    /// sentinel; other entries are renderer-defined direction keywords.
    /// </summary>
    [DataField("variants")]
    public List<string> Variants { get; private set; } = new() { "None" };

    /// <summary>
    /// Index into the marking's <c>Sprites</c> list whose color slot
    /// this gradient's secondary color writes into. The picker uses
    /// this to wire the gradient's enable-when-not-None swatch to the
    /// right entry of <c>Marking.MarkingColors</c>.
    /// </summary>
    [DataField("colorSlotIndex")]
    public int ColorSlotIndex { get; private set; }
}

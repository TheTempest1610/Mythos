using Content.Shared.Humanoid;
using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Mythos.Components;

/// <summary>
/// Mythos sibling component for clothing, opts an item into sex-and-species-aware
/// RSI state resolution. The upstream <c>ClothingComponent</c> remains untouched;
/// the Mythos visuals system subscribes to <c>GetEquipmentVisualsEvent</c> after
/// upstream <c>ClothingSystem</c> and post-processes resolved layer state names
/// to pick the most specific match available in the RSI.
/// </summary>
/// <remarks>
/// Resolution order (most specific wins): {state}-{Sex}-{Species}, {state}-{Species},
/// {state}-{Sex}, {state}. If <see cref="SexStateOverrides"/> is set, it takes
/// priority over the suffix walk for that exact sex.
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class MythosClothingComponent : Component
{
    /// <summary>
    /// If set, when the wearer's <see cref="Sex"/> is a key in this dictionary,
    /// the resolver will substitute the layer state with the value (verbatim)
    /// before applying the suffix-fallback walk. This mirrors the
    /// <c>SexStateOverrides</c> pattern used by <c>VisualOrganComponent</c>.
    /// </summary>
    [DataField]
    public Dictionary<Sex, string>? SexStateOverrides;

    /// <summary>
    /// When true (default), the resolver attempts species-suffix fallback.
    /// Disable for items that should only consider sex variants.
    /// </summary>
    [DataField]
    public bool EnableSpeciesFallback = true;

    /// <summary>
    /// When true (default), the resolver attempts sex-suffix fallback.
    /// Disable for items that should only consider species variants.
    /// </summary>
    [DataField]
    public bool EnableSexFallback = true;
}

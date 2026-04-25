using System.Collections.Generic;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Humanoid.Markings;

/// <summary>
/// Mythos partial extension on the upstream <see cref="MarkingPrototype"/>.
/// The upstream class is <c>sealed partial</c>, so this file lives entirely
/// under the Mythos namespace and adds optional fields without touching the
/// upstream YAML schema or breaking existing markings.
///
/// V1.1 ships only schema; the fields are read but not yet rendered. The
/// chargen UI exposing gradient pickers and the renderer applying gradient
/// blends are V1.2 work. Existing markings without these fields keep the
/// stock per-color-per-sprite behaviour.
/// </summary>
public sealed partial class MarkingPrototype
{
    /// <summary>
    /// Optional gradient slot declarations. Each entry maps an OV-style
    /// gradient (e.g. "Natural", "Dye") onto a sprite color slot, with a
    /// list of named variants ("None", "Fade Up", "Vertical Split", ...).
    /// V1.1 stores user selections via Mythos-side state but the renderer
    /// is a no-op; selecting a gradient does not yet alter the sprite.
    /// </summary>
    [DataField("mythosGradients")]
    public List<MythosGradientSlot>? MythosGradients;
}

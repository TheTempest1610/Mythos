using Content.Shared.Body;
using Robust.Client.Graphics;

namespace Content.Client.Body;

// Mythos: secondary-layer rendering for OV-style organs that need pixels at
// more than one HumanoidVisualLayer (e.g., legs producing a side-view
// background-leg sprite at LLegBehind in addition to LLeg).
public sealed partial class VisualBodySystem
{
    private void ApplyMythosSecondaryLayers(Entity<VisualOrganComponent> ent, EntityUid target)
    {
        if (ent.Comp.SecondaryLayers is not { } layers || layers.Count == 0)
            return;

        var sex = ent.Comp.Profile.Sex;
        foreach (var entry in layers)
        {
            if (!_sprite.LayerMapTryGet(target, entry.Layer, out var index, true))
                continue;

            // Resolve final state per current profile sex (mirrors the primary
            // VisualOrganComponent.SexStateOverrides handling). The entry's
            // Data instance is loaded from YAML and per-entry, so mutating
            // it in place is safe and matches how upstream mutates
            // ent.Comp.Data.State.
            if (entry.SexStateOverrides is { } overrides
                && overrides.TryGetValue(sex, out var sexState))
            {
                entry.Data.State = sexState;
            }

            // Match the primary layer's runtime color (skin tint).
            entry.Data.Color = ent.Comp.Data.Color;
            _sprite.LayerSetData(target, index, entry.Data);
        }
    }

    private void RemoveMythosSecondaryLayers(Entity<VisualOrganComponent> ent, EntityUid target)
    {
        if (ent.Comp.SecondaryLayers is not { } layers || layers.Count == 0)
            return;

        foreach (var entry in layers)
        {
            if (!_sprite.LayerMapTryGet(target, entry.Layer, out var index, true))
                continue;

            _sprite.LayerSetRsiState(target, index, RSI.StateId.Invalid);
        }
    }
}

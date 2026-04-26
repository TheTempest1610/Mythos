using System.Linq;
using Content.Client.DisplacementMap;
using Content.Shared.Body;
using Content.Shared.CCVar;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Body;

// Mythos: split into a partial so behind-body sprite logic can live in a sibling file.
public sealed partial class VisualBodySystem : SharedVisualBodySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly DisplacementMapSystem _displacement = default!;
    [Dependency] private readonly MarkingManager _marking = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VisualOrganComponent, OrganGotInsertedEvent>(OnOrganGotInserted);
        SubscribeLocalEvent<VisualOrganComponent, OrganGotRemovedEvent>(OnOrganGotRemoved);
        SubscribeLocalEvent<VisualOrganComponent, AfterAutoHandleStateEvent>(OnOrganState);

        SubscribeLocalEvent<VisualOrganMarkingsComponent, OrganGotInsertedEvent>(OnMarkingsGotInserted);
        SubscribeLocalEvent<VisualOrganMarkingsComponent, OrganGotRemovedEvent>(OnMarkingsGotRemoved);
        SubscribeLocalEvent<VisualOrganMarkingsComponent, AfterAutoHandleStateEvent>(OnMarkingsState);

        SubscribeLocalEvent<VisualOrganMarkingsComponent, BodyRelayedEvent<HumanoidLayerVisibilityChangedEvent>>(OnMarkingsChangedVisibility);

        Subs.CVar(_cfg, CCVars.AccessibilityClientCensorNudity, OnCensorshipChanged, true);
        Subs.CVar(_cfg, CCVars.AccessibilityServerCensorNudity, OnCensorshipChanged, true);
    }

    private void OnCensorshipChanged(bool value)
    {
        var query = AllEntityQuery<OrganComponent, VisualOrganMarkingsComponent>();
        while (query.MoveNext(out var ent, out var organComp, out var markingsComp))
        {
            if (organComp.Body is not { } body)
                continue;

            RemoveMarkings((ent, markingsComp), body);
            ApplyMarkings((ent, markingsComp), body);
        }
    }

    private void OnOrganGotInserted(Entity<VisualOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        ApplyVisual(ent, args.Target);
    }

    private void OnOrganGotRemoved(Entity<VisualOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        RemoveVisual(ent, args.Target);
    }

    private void OnOrganState(Entity<VisualOrganComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        ApplyVisual(ent, body);
    }

    private void ApplyVisual(Entity<VisualOrganComponent> ent, EntityUid target)
    {
        if (!_sprite.LayerMapTryGet(target, ent.Comp.Layer, out var index, true))
            return;

        _sprite.LayerSetData(target, index, ent.Comp.Data);

        // Mythos: apply secondary sprite layers for OV-derived organs (e.g.,
        // side-view background-leg layer mapped to LLegBehind).
        ApplyMythosSecondaryLayers(ent, target);
    }

    private void RemoveVisual(Entity<VisualOrganComponent> ent, EntityUid target)
    {
        if (!_sprite.LayerMapTryGet(target, ent.Comp.Layer, out var index, true))
            return;

        _sprite.LayerSetRsiState(target, index, RSI.StateId.Invalid);

        // Mythos: clear secondary sprite layers as well.
        RemoveMythosSecondaryLayers(ent, target);
    }

    private void OnMarkingsGotInserted(Entity<VisualOrganMarkingsComponent> ent, ref OrganGotInsertedEvent args)
    {
        ApplyMarkings(ent, args.Target);
    }

    private void OnMarkingsGotRemoved(Entity<VisualOrganMarkingsComponent> ent, ref OrganGotRemovedEvent args)
    {
        RemoveMarkings(ent, args.Target);
    }

    private void OnMarkingsState(Entity<VisualOrganMarkingsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        RemoveMarkings(ent, body);
        ApplyMarkings(ent, body);
    }

    protected override void SetOrganColor(Entity<VisualOrganComponent> ent, Color color)
    {
        base.SetOrganColor(ent, color);

        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        ApplyVisual(ent, body);
    }

    protected override void SetOrganMarkings(Entity<VisualOrganMarkingsComponent> ent, Dictionary<HumanoidVisualLayers, List<Marking>> markings)
    {
        base.SetOrganMarkings(ent, markings);

        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        RemoveMarkings(ent, body);
        ApplyMarkings(ent, body);
    }

    protected override void SetOrganAppearance(Entity<VisualOrganComponent> ent, PrototypeLayerData data)
    {
        base.SetOrganAppearance(ent, data);

        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        ApplyVisual(ent, body);
    }

    private IEnumerable<Marking> AllMarkings(Entity<VisualOrganMarkingsComponent> ent)
    {
        foreach (var markings in ent.Comp.Markings.Values)
        {
            foreach (var marking in markings)
            {
                yield return marking;
            }
        }

        var censorNudity = _cfg.GetCVar(CCVars.AccessibilityClientCensorNudity) || _cfg.GetCVar(CCVars.AccessibilityServerCensorNudity);
        if (!censorNudity)
            yield break;

        var group = _prototype.Index(ent.Comp.MarkingData.Group);
        foreach (var layer in ent.Comp.MarkingData.Layers)
        {
            if (!group.Limits.TryGetValue(layer, out var layerLimits))
                continue;

            if (layerLimits.NudityDefault.Count < 1)
                continue;

            var markings = ent.Comp.Markings.GetValueOrDefault(layer) ?? [];
            if (markings.Any(marking => _marking.TryGetMarking(marking, out var proto) && proto.BodyPart == layer))
                continue;

            foreach (var marking in layerLimits.NudityDefault)
            {
                yield return new(marking, 1);
            }
        }
    }

    private void ApplyMarkings(Entity<VisualOrganMarkingsComponent> ent, Entity<SpriteComponent?> target)
    {
        if (!Resolve(target, ref target.Comp))
            return;

        // Mythos: pre-pass for OV-faithful cross-marking effects.
        //   * covers_breasts mutual exclusion. OV's
        //     /datum/sprite_accessory/breasts/is_visible (genitals.dm:142)
        //     returns FALSE when the wearer's underwear has
        //     covers_breasts; the breast organ overlay is then skipped
        //     entirely. Mirrored here by suppressing any
        //     category=Breasts marking when any other applied marking
        //     has CoversBreasts. Without this, breasts at BodyFrontest
        //     (z=-4) would render above bras at UndergarmentBottom
        //     (z=-43) and visually swallow them.
        //   * Wearer's breast size, used by MatchesBreastSize markings
        //     (bikini, leotard) to synthesize the right per-size
        //     sprite. Mirrors OV's bikini get_icon_state at
        //     underwear.dm:36, which reads owner.breasts.breast_size
        //     and rebuilds "bikini_f_<size>" each render. The size
        //     comes from the breast prototype's MythosOrderIndex,
        //     which the breast consolidator stamps with OV's 0-16
        //     value.
        var coversBreasts = false;
        int? wearerBreastSize = null;
        foreach (var marking in AllMarkings(ent))
        {
            if (!_marking.TryGetMarking(marking, out var coverProto))
                continue;
            if (coverProto.CoversBreasts)
                coversBreasts = true;
            if (coverProto.Category == "Breasts" && coverProto.MythosOrderIndex is { } size)
                wearerBreastSize = size;
        }

        var applied = new List<Marking>();
        foreach (var marking in AllMarkings(ent))
        {
            if (!_marking.TryGetMarking(marking, out var proto))
                continue;

            if (coversBreasts && proto.Category == "Breasts")
                continue;

            // Mythos: bikini / leotard size-match. When MatchesBreastSize
            // is set, route the wearer's breast size into the size-state
            // selector instead of the marking's own MythosSizeIndex
            // (which is unused for these prototypes — they're picked
            // without a size slider). Falls back to the marking's own
            // sizeIndex (typically null) when the wearer has no breast
            // marking, which makes GetActiveSprites return the default
            // Sprites list, mirroring OV's `else return "bikini_f_0"`.
            var sizeIndex = marking.MythosSizeIndex;
            if (proto.MatchesBreastSize && wearerBreastSize is { } bs)
                sizeIndex = bs;

            // Mythos: pick the active sprite list given the marking's
            // current variant / toggle / size state. Defaults to proto.Sprites.
            var sprites = proto.GetActiveSprites(marking.MythosToggles, sizeIndex, marking.MythosVariant);

            for (var i = 0; i < sprites.Count; i++)
            {
                var sprite = sprites[i];

                DebugTools.Assert(sprite is SpriteSpecifier.Rsi);
                if (sprite is not SpriteSpecifier.Rsi rsi)
                    continue;

                // Mythos: per-sprite bodyPart override (defaults to proto.BodyPart).
                // Lets one marking land its sprites on different humanoid layers,
                // e.g., a tail's _behind state on TailBehind and _front on Tail.
                var layer = proto.GetSpriteBodyPart(i);
                if (!_sprite.LayerMapTryGet(target, layer, out var index, true))
                    continue;

                ent.Comp.MarkingsDisplacement.TryGetValue(layer, out var displacement);

                var layerId = $"{proto.ID}-{rsi.RsiState}";

                if (!_sprite.LayerMapTryGet(target, layerId, out _, false))
                {
                    var spriteLayer = _sprite.AddLayer(target, sprite, index + 1);
                    _sprite.LayerMapSet(target, layerId, spriteLayer);
                    _sprite.LayerSetSprite(target, layerId, rsi);
                }

                // Mythos: route to the prototype's color-slot index so paired
                // BEHIND/FRONT sprites of the same OV color slot share a color.
                var colorIndex = proto.GetSpriteColorIndex(i);
                if (marking.MarkingColors is not null && colorIndex < marking.MarkingColors.Count)
                    _sprite.LayerSetColor(target, layerId, marking.MarkingColors[colorIndex]);
                else
                    _sprite.LayerSetColor(target, layerId, Color.White);

                if (displacement != null && proto.CanBeDisplaced)
                    _displacement.TryAddDisplacement(displacement, (target, target.Comp), index + 1, layerId, out _);
            }

            applied.Add(marking);
        }
        ent.Comp.AppliedMarkings = applied;
    }

    private void RemoveMarkings(Entity<VisualOrganMarkingsComponent> ent, Entity<SpriteComponent?> target)
    {
        if (!Resolve(target, ref target.Comp))
            return;

        foreach (var marking in ent.Comp.AppliedMarkings)
        {
            if (!_marking.TryGetMarking(marking, out var proto))
                continue;

            // Mythos: clear the active sprite list (the one Apply added).
            // Default sprite list still gets cleared as a safety net to
            // catch markings that were applied with a now-different
            // toggle / size state.
            var activeSprites = proto.GetActiveSprites(marking.MythosToggles, marking.MythosSizeIndex, marking.MythosVariant);
            var spriteListsCollected = new List<List<SpriteSpecifier>> { activeSprites };
            if (!ReferenceEquals(activeSprites, proto.Sprites))
                spriteListsCollected.Add(proto.Sprites);
            // Mythos: MatchesBreastSize markings (bikini, leotard) pick
            // their actual rendered state from the wearer's breast size
            // at Apply time, not from the marking's own MythosSizeIndex
            // (which stays null). When the breast size changes, the
            // previously-applied size variant's layer still lives in
            // the sprite under its size-specific layerId, but
            // GetActiveSprites can no longer find it. Sweep every size
            // variant to clean up stale layers.
            if (proto.MatchesBreastSize && proto.MythosSizeStates is { } sizes)
                spriteListsCollected.AddRange(sizes);
            var spriteLists = spriteListsCollected;
            foreach (var spriteList in spriteLists)
            foreach (var sprite in spriteList)
            {
                DebugTools.Assert(sprite is SpriteSpecifier.Rsi);
                if (sprite is not SpriteSpecifier.Rsi rsi)
                    continue;

                var layerId = $"{proto.ID}-{rsi.RsiState}";

                // If this marking is one that can be displaced, we need to remove the displacement as well; otherwise
                // altering a marking at runtime can lead to the renderer falling over.
                // The Vulps must be shaved.
                // (https://github.com/space-wizards/space-station-14/issues/40135).
                if (proto.CanBeDisplaced)
                    _displacement.EnsureDisplacementIsNotOnSprite((target, target.Comp), layerId);

                if (!_sprite.LayerMapTryGet(target, layerId, out var index, false))
                    continue;

                _sprite.LayerMapRemove(target, layerId);
                _sprite.RemoveLayer(target, index);
            }
        }
    }

    private void OnMarkingsChangedVisibility(Entity<VisualOrganMarkingsComponent> ent, ref BodyRelayedEvent<HumanoidLayerVisibilityChangedEvent> args)
    {
        if (!ent.Comp.HideableLayers.Contains(args.Args.Layer))
            return;

        foreach (var markings in ent.Comp.Markings.Values)
        {
            foreach (var marking in markings)
            {
                if (!_marking.TryGetMarking(marking, out var proto))
                    continue;

                if (proto.BodyPart != args.Args.Layer && !(ent.Comp.DependentHidingLayers.TryGetValue(args.Args.Layer, out var dependent) && dependent.Contains(proto.BodyPart)))
                    continue;

                foreach (var sprite in proto.Sprites)
                {
                    DebugTools.Assert(sprite is SpriteSpecifier.Rsi);
                    if (sprite is not SpriteSpecifier.Rsi rsi)
                        continue;

                    var layerId = $"{proto.ID}-{rsi.RsiState}";

                    if (!_sprite.LayerMapTryGet(args.Body.Owner, layerId, out var index, true))
                        continue;

                    _sprite.LayerSetVisible(args.Body.Owner, index, args.Args.Visible);
                }
            }
        }
    }
}

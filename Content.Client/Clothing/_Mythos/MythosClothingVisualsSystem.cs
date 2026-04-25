using System.Diagnostics.CodeAnalysis;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Clothing.Mythos;
using Content.Shared.Clothing.Mythos.Components;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Client.Clothing.Mythos;

/// <summary>
/// Mythos sibling system that augments the upstream <see cref="ClothingSystem"/>
/// equipment-visuals pipeline with sex-and-species-aware state resolution. Runs
/// AFTER upstream <see cref="ClothingSystem"/> so it can post-process the layer
/// list with the most specific RSI state available.
/// </summary>
/// <remarks>
/// Only entities carrying <see cref="MythosClothingComponent"/> are affected;
/// vanilla SS14 clothing renders identically to before. The resolver implements
/// the four-step fallback chain (sex+species, species, sex, default) plus an
/// optional <see cref="MythosClothingComponent.SexStateOverrides"/> shortcut
/// that takes priority for the matching sex.
/// </remarks>
public sealed class MythosClothingVisualsSystem : EntitySystem
{
    [Dependency] private readonly IResourceCache _cache = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Subscribe AFTER upstream ClothingSystem so we see the resolved layer
        // list and can rewrite the state field in place. This mirrors the
        // ordering used by FlippableClothingVisualizerSystem.
        SubscribeLocalEvent<MythosClothingComponent, GetEquipmentVisualsEvent>(
            OnGetVisuals,
            after: [typeof(ClothingSystem)]);
    }

    private void OnGetVisuals(Entity<MythosClothingComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (args.Layers.Count == 0)
            return;

        if (!TryComp(args.Equipee, out InventoryComponent? inventory))
            return;

        // Wearer sex from HumanoidProfileComponent (matches ClientClothingSystem
        // line ~274 which reads HumanoidProfileComponent.Sex for displacement maps).
        var sex = CompOrNull<HumanoidProfileComponent>(args.Equipee)?.Sex ?? Sex.Unsexed;
        var species = inventory.SpeciesId;

        // Resolve the RSI for state-existence checks. We try, in order: each
        // layer's own RsiPath, the clothing item's RsiPath, the entity's BaseRSI.
        if (!TryResolveItemRsi(ent, out var fallbackRsi))
            fallbackRsi = null;

        for (var i = 0; i < args.Layers.Count; i++)
        {
            var (key, data) = args.Layers[i];
            if (data.State == null)
                continue;

            var rsi = ResolveLayerRsi(data, fallbackRsi);
            if (rsi == null)
                continue;

            var startingState = data.State;

            // Apply explicit per-sex override first if present.
            if (ent.Comp.SexStateOverrides is { } overrides
                && overrides.TryGetValue(sex, out var overrideState)
                && rsi.TryGetState(overrideState, out _))
            {
                startingState = overrideState;
            }

            // Now run the four-step fallback walk on the (possibly overridden)
            // base state. The walk is a no-op if no more-specific state exists,
            // i.e. backward-compatible for any item without sex/species variants.
            var resolved = MythosClothingStateResolver.Resolve(
                startingState,
                sex,
                species,
                candidate => rsi.TryGetState(candidate, out _),
                ent.Comp.EnableSpeciesFallback,
                ent.Comp.EnableSexFallback);

            if (resolved != data.State)
            {
                data.State = resolved;
                // Casing/existence assertion: the resolver only returns states
                // that pass the existence predicate, OR the original baseState.
                // If we returned the original, it's guaranteed valid because it
                // was produced by the upstream ClothingSystem layer pipeline.
                DebugTools.Assert(
                    rsi.TryGetState(resolved, out _) || resolved == startingState,
                    $"MythosClothingVisualsSystem resolved state '{resolved}' which is not present in RSI '{rsi.Path}'.");
            }
        }
    }

    /// <summary>
    /// Resolve the RSI to consult for state-existence checks for a given layer.
    /// Layers can override RsiPath; otherwise we fall back to the item's own RSI.
    /// </summary>
    private RSI? ResolveLayerRsi(PrototypeLayerData data, RSI? fallbackRsi)
    {
        if (data.RsiPath != null)
        {
            try
            {
                return _cache.GetResource<RSIResource>(
                    SpriteSpecifierSerializer.TextureRoot / data.RsiPath).RSI;
            }
            catch
            {
                // RSI failed to load; fall through to the item-level fallback.
            }
        }
        return fallbackRsi;
    }

    /// <summary>
    /// Resolve the clothing item's own RSI (via <see cref="ClothingComponent.RsiPath"/>
    /// or its <see cref="SpriteComponent"/>'s base RSI).
    /// </summary>
    private bool TryResolveItemRsi(EntityUid uid, [NotNullWhen(true)] out RSI? rsi)
    {
        rsi = null;

        if (TryComp(uid, out ClothingComponent? clothing) && clothing.RsiPath != null)
        {
            try
            {
                rsi = _cache.GetResource<RSIResource>(
                    SpriteSpecifierSerializer.TextureRoot / clothing.RsiPath).RSI;
                return true;
            }
            catch
            {
                // Fall through.
            }
        }

        if (TryComp(uid, out SpriteComponent? sprite) && sprite.BaseRSI != null)
        {
            rsi = sprite.BaseRSI;
            return true;
        }

        return false;
    }
}

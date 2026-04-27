// Mythos: chargen "Clothing" tab persistence on the character profile.
//
// MythosClothingSelections is a slot-name -> entity-prototype-ID map
// of the items the player picked in the chargen Clothing tab. The
// [DataField] tag handles the network/in-memory round-trip, but DB
// persistence is hand-written EF Core: see the new column on the
// Profile entity (Content.Server.Database/Model.cs) plus the save
// branch in ServerDbBase.ConvertProfiles and the load branch in
// ServerPreferencesManager.ConvertProfiles. Add to all three when
// extending this dictionary's shape. The server spawn path
// (Content.Server/_Mythos/Clothing/MythosClothingSpawnSystem.cs)
// reads it after job-loadout equip to apply the player's choices.
//
// Slot KEY is the inventory-template ``name`` field (string), not
// the SlotFlags enum value. That stays stable across enum reordering
// and matches what InventorySystem.TryEquip expects.
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    /// <summary>
    /// Map of inventory slot name -> entity prototype ID for items the
    /// player picked in the chargen "Clothing" tab. Empty for non-Mythos
    /// species or when the player picked nothing.
    /// </summary>
    [DataField]
    public Dictionary<string, EntProtoId> MythosClothingSelections { get; private set; }
        = new();

    /// <summary>
    /// Functional update: returns a copy of this profile with
    /// <see cref="MythosClothingSelections"/> replaced by the given map.
    /// Mirrors the existing <see cref="WithCharacterAppearance"/> /
    /// loadout pattern so chargen save calls compose with other
    /// profile mutations.
    /// </summary>
    public HumanoidCharacterProfile WithMythosClothing(
        IReadOnlyDictionary<string, EntProtoId> selections)
    {
        var copy = new HumanoidCharacterProfile(this);
        copy.MythosClothingSelections = new Dictionary<string, EntProtoId>(selections);
        return copy;
    }

    /// <summary>
    /// Mythos: hook called from the upstream copy constructor so
    /// partial-class-owned fields survive the With* round-trip. Without
    /// this, every WithName/WithSex/... call would silently reset
    /// MythosClothingSelections to an empty dict because the upstream
    /// copy ctor only chains the canonical positional ctor.
    /// </summary>
    partial void CopyMythosFieldsFrom(HumanoidCharacterProfile other)
    {
        MythosClothingSelections =
            new Dictionary<string, EntProtoId>(other.MythosClothingSelections);
    }

    /// <summary>
    /// Mythos: equality hook for fields owned by the Mythos partial.
    /// Returning false here flags the profile as dirty in the chargen
    /// editor's <c>SetDirty</c> bookkeeping, which is the trigger that
    /// enables the Save button after a clothing-tab edit.
    /// </summary>
    private partial bool MythosMemberwiseEquals(HumanoidCharacterProfile other)
    {
        if (MythosClothingSelections.Count != other.MythosClothingSelections.Count)
            return false;
        foreach (var (slotName, protoId) in MythosClothingSelections)
        {
            if (!other.MythosClothingSelections.TryGetValue(slotName, out var otherId))
                return false;
            if (!protoId.Equals(otherId))
                return false;
        }
        return true;
    }
}

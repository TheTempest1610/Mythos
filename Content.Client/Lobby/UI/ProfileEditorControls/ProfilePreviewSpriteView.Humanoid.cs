using System.Linq;
using Content.Client.Humanoid;
using Content.Client.Station;
using Content.Shared.Body;
using Content.Shared.Clothing;
using Content.Shared.Clothing._Mythos;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

public sealed partial class ProfilePreviewSpriteView
{
    /// <summary>
    /// A slim reload that only updates the entity itself and not any of the job entities, etc.
    /// </summary>
    private void ReloadHumanoidEntity(HumanoidCharacterProfile humanoid)
    {
        if (!EntMan.EntityExists(PreviewDummy) ||
            !EntMan.HasComponent<VisualBodyComponent>(PreviewDummy))
            return;

        EntMan.System<SharedVisualBodySystem>().ApplyProfileTo(PreviewDummy, humanoid);
    }

    /// <summary>
    /// Loads the profile onto a dummy entity.
    /// </summary>
    private void LoadHumanoidEntity(HumanoidCharacterProfile? humanoid, JobPrototype? job, bool jobClothes)
    {
        EntProtoId? previewEntity = null;
        if (humanoid != null && jobClothes)
        {
            job ??= GetPreferredJob(humanoid);

            previewEntity = job.JobPreviewEntity ?? (EntProtoId?)job?.JobEntity;
        }

        if (previewEntity != null)
        {
            // Special type like borg or AI, do not spawn a human just spawn the entity.
            PreviewDummy = EntMan.SpawnEntity(previewEntity, MapCoordinates.Nullspace);
        }
        else if (humanoid is not null)
        {
            var dummy = _prototypeManager.Index(humanoid.Species).DollPrototype;
            PreviewDummy = EntMan.SpawnEntity(dummy, MapCoordinates.Nullspace);
            EntMan.System<SharedVisualBodySystem>().ApplyProfileTo(PreviewDummy, humanoid);
        }
        else
        {
            PreviewDummy = EntMan.SpawnEntity(_prototypeManager.Index(HumanoidCharacterProfile.DefaultSpecies).DollPrototype, MapCoordinates.Nullspace);
        }

        if (humanoid != null && jobClothes)
        {
            DebugTools.Assert(job != null);

            GiveDummyJobClothes(humanoid, job);

            if (_prototypeManager.HasIndex<RoleLoadoutPrototype>(LoadoutSystem.GetJobPrototype(job.ID)))
            {
                var loadout = humanoid.GetLoadoutOrDefault(LoadoutSystem.GetJobPrototype(job.ID), _playerManager.LocalSession, humanoid.Species, EntMan, _prototypeManager);
                GiveDummyLoadout(loadout);
            }
        }
    }

    /// <summary>
    /// Equip / unequip Mythos clothing-tab selections on the existing
    /// preview dummy. Lightweight (no respawn) -- mirrors how
    /// <see cref="ReloadHumanoidEntity"/> updates appearance for
    /// markings-tab changes. Iterates only the slots
    /// <see cref="MythosClothingPicker.ManagedSlots"/> declares; non-
    /// managed slots (POCKETs, IDCARD, ...) are left untouched.
    /// </summary>
    /// <param name="selections">
    /// Snapshot from <see cref="MythosClothingPicker.Selections"/>.
    /// Slots present in the dict get equipped (replacing any current
    /// item); managed slots NOT in the dict get cleared (so deselecting
    /// an item visually unequips it).
    /// </param>
    /// <param name="managedSlots">
    /// The full set of slots the Mythos picker controls (typically
    /// <see cref="MythosClothingPicker.ManagedSlots"/>). Threaded in
    /// rather than referenced statically so this file stays free of a
    /// dependency on the Mythos client UI assembly's picker class.
    /// </param>
    /// <summary>
    /// Mythos: profile-driven equip helper. Translates the
    /// name-keyed <see cref="HumanoidCharacterProfile.MythosClothingSelections"/>
    /// (the on-disk shape) into the SlotFlags-keyed shape
    /// <see cref="ApplyMythosClothing(IReadOnlyDictionary{SlotFlags, EntityPrototype}, IReadOnlyCollection{SlotFlags})"/>
    /// expects, and re-equips. Used by the character-selection menu
    /// and the lobby idle preview where there's no live picker
    /// instance to read selections from.
    /// </summary>
    /// <remarks>
    /// Missing prototypes (renamed / removed since the profile was
    /// saved) are silently skipped; the slot is still cleared so the
    /// preview stays consistent with what the server-side spawn path
    /// will do.
    /// </remarks>
    public void ApplyMythosClothingFromProfile(HumanoidCharacterProfile profile)
    {
        var selections = new Dictionary<SlotFlags, EntityPrototype>(
            profile.MythosClothingSelections.Count);
        foreach (var (slotName, protoId) in profile.MythosClothingSelections)
        {
            var flag = MythosClothingSlots.NameToFlag(slotName);
            if (flag == SlotFlags.NONE)
                continue;
            if (!_prototypeManager.TryIndex<EntityPrototype>(protoId, out var proto))
                continue;
            selections[flag] = proto;
        }

        ApplyMythosClothing(selections,
            Content.Client._Mythos.Lobby.MythosClothingPicker.ManagedSlots);
    }

    public void ApplyMythosClothing(
        IReadOnlyDictionary<SlotFlags, EntityPrototype> selections,
        IReadOnlyCollection<SlotFlags> managedSlots)
    {
        if (!EntMan.EntityExists(PreviewDummy))
            return;

        var inventorySys = EntMan.System<InventorySystem>();
        if (!inventorySys.TryGetSlots(PreviewDummy, out var slots))
            return;

        foreach (var slot in slots)
        {
            if (!managedSlots.Contains(slot.SlotFlags))
                continue;

            // Always unequip whatever's there; we own this slot.
            if (inventorySys.TryUnequip(PreviewDummy, slot.Name,
                    out var existing, silent: true, force: true,
                    reparent: false))
            {
                EntMan.DeleteEntity(existing.Value);
            }

            // Equip the selection if one exists for this slot's flag.
            if (selections.TryGetValue(slot.SlotFlags, out var proto))
            {
                var item = EntMan.SpawnEntity(proto.ID, MapCoordinates.Nullspace);
                inventorySys.TryEquip(PreviewDummy, item, slot.Name,
                    silent: true, force: true);
            }
        }
    }

    /// <summary>
    /// Gets the highest priority job for the profile.
    /// </summary>
    private JobPrototype GetPreferredJob(HumanoidCharacterProfile profile)
    {
        var highPriorityJob = profile.JobPriorities.FirstOrDefault(p => p.Value == JobPriority.High).Key;
        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract (what is resharper smoking?)
        return _prototypeManager.Index<JobPrototype>(highPriorityJob.Id ?? SharedGameTicker.FallbackOverflowJob);
    }

    private void GiveDummyLoadout(RoleLoadout? roleLoadout)
    {
        if (roleLoadout == null)
            return;

        var spawnSys = EntMan.System<StationSpawningSystem>();

        foreach (var group in roleLoadout.SelectedLoadouts.Values)
        {
            foreach (var loadout in group)
            {
                if (!_prototypeManager.Resolve(loadout.Prototype, out var loadoutProto))
                    continue;

                spawnSys.EquipStartingGear(PreviewDummy, loadoutProto);
            }
        }
    }

    /// <summary>
    /// Applies the specified job's clothes to the dummy.
    /// </summary>
    private void GiveDummyJobClothes(HumanoidCharacterProfile profile, JobPrototype job)
    {
        var inventorySys = EntMan.System<InventorySystem>();
        if (!inventorySys.TryGetSlots(PreviewDummy, out var slots))
            return;

        // Apply loadout
        if (profile.Loadouts.TryGetValue(job.ID, out var jobLoadout))
        {
            foreach (var loadouts in jobLoadout.SelectedLoadouts.Values)
            {
                foreach (var loadout in loadouts)
                {
                    if (!_prototypeManager.Resolve(loadout.Prototype, out var loadoutProto))
                        continue;

                    // TODO: Need some way to apply starting gear to an entity and replace existing stuff coz holy fucking shit dude.
                    foreach (var slot in slots)
                    {
                        // Try startinggear first
                        if (_prototypeManager.Resolve(loadoutProto.StartingGear, out var loadoutGear))
                        {
                            var itemType = ((IEquipmentLoadout) loadoutGear).GetGear(slot.Name);

                            if (inventorySys.TryUnequip(PreviewDummy, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
                            {
                                EntMan.DeleteEntity(unequippedItem.Value);
                            }

                            if (itemType != string.Empty)
                            {
                                var item = EntMan.SpawnEntity(itemType, MapCoordinates.Nullspace);
                                inventorySys.TryEquip(PreviewDummy, item, slot.Name, true, true);
                            }
                        }
                        else
                        {
                            var itemType = ((IEquipmentLoadout) loadoutProto).GetGear(slot.Name);

                            if (inventorySys.TryUnequip(PreviewDummy, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
                            {
                                EntMan.DeleteEntity(unequippedItem.Value);
                            }

                            if (itemType != string.Empty)
                            {
                                var item = EntMan.SpawnEntity(itemType, MapCoordinates.Nullspace);
                                inventorySys.TryEquip(PreviewDummy, item, slot.Name, true, true);
                            }
                        }
                    }
                }
            }
        }

        if (!_prototypeManager.Resolve(job.StartingGear, out var gear))
            return;

        foreach (var slot in slots)
        {
            var itemType = ((IEquipmentLoadout) gear).GetGear(slot.Name);

            if (inventorySys.TryUnequip(PreviewDummy, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
            {
                EntMan.DeleteEntity(unequippedItem.Value);
            }

            if (itemType != string.Empty)
            {
                var item = EntMan.SpawnEntity(itemType, MapCoordinates.Nullspace);
                inventorySys.TryEquip(PreviewDummy, item, slot.Name, true, true);
            }
        }
    }
}

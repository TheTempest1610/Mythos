// Mythos: server-side spawn hook that equips a player's chargen
// "Clothing" tab selections after the upstream job-loadout pass has
// finished. Subscribes to PlayerSpawnCompleteEvent so the timing is the
// same as TraitSystem's hook -- player is fully spawned, has an
// inventory, and (in modes where it ran) has already been dressed in
// vanilla loadout items.
//
// Behaviour is gated on the `mythos.clothing.mode` CVar:
//   * "mythos" (default): strip every inventory slot the upstream job
//     loadout populated (jumpsuit, PDA, ID, backpack, belt items,
//     pockets, suit storage, ...) and replace with the player's
//     chargen selections. The player keeps their hands and any
//     non-inventory components (mob state, traits, etc.); only the
//     SS14-style starting kit is replaced.
//   * "ss14": no-op. Vanilla loadout stays exactly as it ran.
//   * "both": layer the player's selections on top of vanilla loadout
//     -- only slots the player actually picked something for get
//     overridden. Useful for porting where a job's tools should stay
//     but the cosmetic shell is the player's call.
//
// Missing prototypes (renamed / removed since the profile was saved)
// are logged to the `mythos.clothing.spawn` sawmill and silently
// skipped, matching the picker's "soft skip with logging" policy.
using Content.Shared.CCVar;
using Content.Shared.Clothing._Mythos;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server.Mythos.Clothing;

public sealed class MythosClothingSpawnSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        var mode = _cfg.GetCVar(CCVars.MythosClothingMode);
        if (string.Equals(mode, "ss14", System.StringComparison.OrdinalIgnoreCase))
            return;

        // "mythos" clears every managed slot first; "both" only touches
        // the slots the player actually selected an item for. Any other
        // value falls back to "mythos" semantics with a warning, so an
        // operator typo doesn't silently disable Mythos chargen.
        var clearUnselected = !string.Equals(mode, "both",
            System.StringComparison.OrdinalIgnoreCase);
        if (clearUnselected
            && !string.Equals(mode, "mythos", System.StringComparison.OrdinalIgnoreCase))
        {
            Log.Warning(
                $"Unknown {CCVars.MythosClothingMode.Name} value '{mode}'; "
                + $"falling back to 'mythos' semantics.");
        }

        var profile = args.Profile;
        var mob = args.Mob;
        var selections = profile.MythosClothingSelections;

        // In "mythos" mode, strip every inventory slot the player has,
        // not just the Mythos picker's managed clothing slots. This is
        // what kills the SS14 starting kit (PDA, ID, backpack, belt,
        // pockets, suit storage). Hands are not inventory slots so they
        // are unaffected.
        //
        // Important: delete the contained entities directly rather than
        // routing through TryUnequip. TryUnequip cascades into slots
        // declared with `dependsOn` (pocket1/pocket2/id depend on
        // jumpsuit; suitstorage depends on outerClothing) and that
        // cascade reparents the dependent contents to the player's
        // map -- a PDA dumped at the player's feet on spawn. Deleting
        // in place, with the entity still parented to its slot
        // container, sidesteps the cascade entirely.
        if (clearUnselected && _inventory.TryGetSlots(mob, out var slots))
        {
            foreach (var slot in slots)
            {
                if (_inventory.TryGetSlotEntity(mob, slot.Name, out var existing))
                    Del(existing.Value);
            }
        }

        // Equip the player's chargen Clothing-tab selections.
        foreach (var (_, slotName) in MythosClothingSlots.All)
        {
            if (!selections.TryGetValue(slotName, out var protoId))
            {
                // In "both" mode, an empty selection means "don't
                // touch this slot"; the vanilla loadout item stays.
                // In "mythos" mode the slot was already cleared above.
                continue;
            }

            if (!_prototypes.HasIndex<EntityPrototype>(protoId))
            {
                Log.Warning(
                    $"Profile for {args.Player.Name} references missing "
                    + $"prototype '{protoId}' in slot '{slotName}'; "
                    + $"skipping equip.");
                continue;
            }

            // In "both" mode the slot may still hold a job-loadout item;
            // unequip it so our selection wins.
            if (!clearUnselected
                && _inventory.TryUnequip(mob, slotName, out var existing,
                    silent: true, force: true, reparent: false))
            {
                Del(existing.Value);
            }

            var coords = Transform(mob).Coordinates;
            var item = Spawn(protoId, coords);
            if (!_inventory.TryEquip(mob, item, slotName,
                    silent: true, force: true))
            {
                Log.Warning(
                    $"Failed to equip '{protoId}' on {args.Player.Name} "
                    + $"in slot '{slotName}'; deleting orphan item.");
                Del(item);
            }
        }
    }
}

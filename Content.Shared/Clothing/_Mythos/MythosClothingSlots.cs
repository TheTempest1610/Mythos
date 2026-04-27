// Mythos: shared registry of inventory slots the chargen "Clothing"
// tab manages. Both the client picker (MythosClothingPicker) and the
// server spawn system (MythosClothingSpawnSystem) iterate this list,
// so it lives in Content.Shared. Adding or removing a Mythos-managed
// slot only requires editing this one table.
using Content.Shared.Inventory;

namespace Content.Shared.Clothing._Mythos;

public static class MythosClothingSlots
{
    /// <summary>
    /// SlotFlags <-> inventory-template slot-name pairs for every slot
    /// the chargen "Clothing" tab can populate. Order is roughly
    /// head-to-feet so picker tabs render in a predictable sequence.
    /// </summary>
    /// <remarks>
    /// The slot Name is the ``name:`` field from
    /// Resources/Prototypes/InventoryTemplates/human_inventory_template.yml.
    /// It is what <see cref="InventorySystem.TryEquip"/> takes as the
    /// slot identifier when the spawn system equips the player's saved
    /// selections. Keep in sync if either side changes.
    /// </remarks>
    public static readonly (SlotFlags Flag, string Name)[] All =
    {
        (SlotFlags.HEAD,          "head"),
        (SlotFlags.MASK,          "mask"),
        (SlotFlags.NECK,          "neck"),
        (SlotFlags.CLOAK,         "cloak"),
        (SlotFlags.OUTERCLOTHING, "outerClothing"),
        (SlotFlags.INNERCLOTHING, "jumpsuit"),
        (SlotFlags.BELT,          "belt"),
        (SlotFlags.BACK,          "back"),
        (SlotFlags.GLOVES,        "gloves"),
        (SlotFlags.LEGS,          "pants"),
        (SlotFlags.FEET,          "shoes"),
        (SlotFlags.EYES,          "eyes"),
        (SlotFlags.EARS,          "ears"),
    };

    /// <summary>
    /// Convert a <see cref="SlotFlags"/> value to its
    /// inventory-template slot name, or null if the flag is not a
    /// Mythos-managed slot.
    /// </summary>
    public static string? FlagToName(SlotFlags flag)
    {
        foreach (var (f, n) in All)
        {
            if (f == flag)
                return n;
        }
        return null;
    }

    /// <summary>
    /// Convert an inventory-template slot name to its
    /// <see cref="SlotFlags"/> value, or <c>SlotFlags.NONE</c> if the
    /// name is not a Mythos-managed slot.
    /// </summary>
    public static SlotFlags NameToFlag(string name)
    {
        foreach (var (f, n) in All)
        {
            if (n == name)
                return f;
        }
        return SlotFlags.NONE;
    }
}

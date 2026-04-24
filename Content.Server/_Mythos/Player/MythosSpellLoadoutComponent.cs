namespace Content.Server.Mythos.Player;

/// <summary>
/// Marker component added to players once the Mythos spell loadout
/// (ManaComponent + spell action entities) has been granted. Subsequent
/// <c>PlayerAttachedEvent</c> fires (reconnection, entity re-attach, etc.)
/// are no-ops when this marker is present, so the loadout isn't granted
/// twice and the action bar doesn't gain duplicate entries.
/// </summary>
[RegisterComponent]
public sealed partial class MythosSpellLoadoutComponent : Component
{
}

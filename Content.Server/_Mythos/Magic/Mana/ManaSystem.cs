using Content.Shared.Mythos.Magic.Mana;

namespace Content.Server.Mythos.Magic.Mana;

/// <summary>
/// Server-side registration shell for the mana system. All mana math lives
/// in <see cref="SharedManaSystem"/> so that predictive events mutate state
/// identically on both sides.
/// </summary>
public sealed class ManaSystem : SharedManaSystem
{
}

using Content.Shared.Mythos.Magic.Mana;

namespace Content.Client.Mythos.Magic.Mana;

/// <summary>
/// Client-side registration shell for the mana system. All mana math lives
/// in <see cref="SharedManaSystem"/> so that predictive events mutate state
/// identically on both sides.
/// </summary>
public sealed class ManaSystem : SharedManaSystem
{
}

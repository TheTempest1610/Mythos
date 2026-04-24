using Content.Shared.Mythos.Magic.Fireball;

namespace Content.Client.Mythos.Magic.Fireball;

/// <summary>
/// Client-side registration shell. Fireball is server-driven (see
/// <c>Content.Server/_Mythos/Magic/Fireball/FireballSystem.cs</c>) because
/// <c>SharedDoAfterSystem</c> ownership and damage application are both
/// server-authoritative; the client observes cast progress via the stock
/// <c>DoAfterComponent</c> state replication, which gives a networked
/// progress bar for free.
///
/// Visual-only cast effects (sparkles at the target tile, incantation
/// sound) will land here in a later polish pass.
/// </summary>
public sealed class FireballSystem : SharedFireballSystem
{
}

using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Client.Mythos.Combat.Targeting;

/// <summary>
/// Registers and tears down the target reticle overlay.
/// </summary>
public sealed class TargetReticleOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay.AddOverlay(new TargetReticleOverlay(EntityManager, _player));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<TargetReticleOverlay>();
    }
}

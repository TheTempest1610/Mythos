using Content.Shared.Mythos.Combat.Targeting;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;

namespace Content.Client.Mythos.Combat.Targeting;

/// <summary>
/// Draws a simple red ring around the local player's currently-selected combat
/// target. Placeholder visual, to be replaced with a proper sprite / icon later.
/// </summary>
public sealed class TargetReticleOverlay : Overlay
{
    private readonly IEntityManager _entMan;
    private readonly IPlayerManager _player;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public TargetReticleOverlay(IEntityManager entMan, IPlayerManager player)
    {
        _entMan = entMan;
        _player = player;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_player.LocalEntity is not { } local)
            return;

        if (!_entMan.TryGetComponent<CombatTargetComponent>(local, out var targetComp))
            return;

        if (targetComp.Target is not { } target)
            return;

        if (!_entMan.TryGetComponent<TransformComponent>(target, out var xform))
            return;

        if (xform.MapID != args.MapId)
            return;

        var xformSys = _entMan.System<SharedTransformSystem>();
        var pos = xformSys.GetWorldPosition(xform);

        var handle = args.WorldHandle;
        handle.DrawCircle(pos, 0.50f, Color.Red.WithAlpha(0.30f), filled: false);
        handle.DrawCircle(pos, 0.55f, Color.Red.WithAlpha(0.60f), filled: false);
    }
}

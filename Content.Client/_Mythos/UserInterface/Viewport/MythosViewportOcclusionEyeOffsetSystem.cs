using System.Numerics;
using Content.Client.UserInterface.Screens;
using Content.Client.Viewport;
using Content.Shared.Camera;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Map;

namespace Content.Client.Mythos.UserInterface.Viewport;

public sealed class MythosViewportOcclusionEyeOffsetSystem : EntitySystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EyeComponent, GetEyeOffsetEvent>(OnGetEyeOffset);
    }

    private void OnGetEyeOffset(Entity<EyeComponent> ent, ref GetEyeOffsetEvent args)
    {
        if (ent.Owner != _player.LocalEntity)
            return;

        if (_ui.ActiveScreen is not InGameScreen screen)
            return;

        if (_eyeManager.MainViewport is not ScalingViewport viewport)
            return;

        var (leftOcclusion, rightOcclusion) = screen.GetMythosViewportOcclusionPixels();
        if (leftOcclusion <= 0f && rightOcclusion <= 0f)
            return;

        var viewportLeft = viewport.GlobalPixelPosition.X;
        var viewportRight = viewportLeft + viewport.PixelWidth;
        var viewportTop = viewport.GlobalPixelPosition.Y;
        var viewportBottom = viewportTop + viewport.PixelHeight;

        var visibleLeft = MathF.Min(viewportRight, viewportLeft + leftOcclusion);
        var visibleRight = MathF.Max(visibleLeft, viewportRight - rightOcclusion);
        var visibleCenter = new Vector2((visibleLeft + visibleRight) * 0.5f, (viewportTop + viewportBottom) * 0.5f);
        var viewportCenter = new Vector2((viewportLeft + viewportRight) * 0.5f, visibleCenter.Y);

        if (Vector2.DistanceSquared(visibleCenter, viewportCenter) < 0.01f)
            return;

        var centerMap = viewport.PixelToMap(viewportCenter);
        var visibleCenterMap = viewport.PixelToMap(visibleCenter);

        if (centerMap.MapId == MapId.Nullspace || centerMap.MapId != visibleCenterMap.MapId)
            return;

        args.Offset += centerMap.Position - visibleCenterMap.Position;
    }
}

using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Content.Client.Mythos.UserInterface.OldHud;

namespace Content.Client.Mythos.UserInterface.QueueHud;

/// <summary>
/// Registers the Mythos combat-queue HUD overlay on startup and tears it
/// down on shutdown. The overlay itself is stateless: it polls the local
/// player's <c>CombatQueueComponent</c> each frame, so nothing per-tick
/// happens here.
/// </summary>
public sealed class CombatQueueHudOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        var oldHud = _ui.GetUIController<OldHudVisibilityUIController>();
        _overlay.AddOverlay(new CombatQueueHudOverlay(EntityManager, _player, _ui, _resourceCache, oldHud));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<CombatQueueHudOverlay>();
    }
}

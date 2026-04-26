using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Content.Client.Mythos.UserInterface.OldHud;

namespace Content.Client.Mythos.UserInterface.ManaHud;

/// <summary>
/// Registers the Mythos mana HUD overlay when the client system starts and
/// tears it down on shutdown. The overlay itself polls <c>ManaComponent</c>
/// on the local player each frame, so there is no per-tick work here.
/// </summary>
public sealed class ManaHudOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        var oldHud = _ui.GetUIController<OldHudVisibilityUIController>();
        _overlay.AddOverlay(new ManaHudOverlay(EntityManager, _player, _ui, _resourceCache, oldHud));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlay.RemoveOverlay<ManaHudOverlay>();
    }
}

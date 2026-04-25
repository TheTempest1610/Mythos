using System.Numerics;
using Content.Client.Administration.Managers;
using Content.Client._Mythos.TileSpawn; // Mythos: variant-aware tile spawn panel
using Content.Client.Gameplay;
using Content.Client.Sandbox;
using Content.Client.UserInterface.Controllers;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.DecalPlacer;
using Content.Client.UserInterface.Systems.Sandbox.Windows;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controllers.Implementations;
using Robust.Shared.Console;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Systems.Sandbox;

// TODO hud refactor should part of this be in engine?
[UsedImplicitly]
public sealed class SandboxUIController : UIController, IOnStateChanged<GameplayState>, IOnSystemChanged<SandboxSystem>
{
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IClientAdminManager _admin = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    [UISystemDependency] private readonly SandboxSystem _sandbox = default!;

    private SandboxWindow? _window;

    // TODO hud refactor cache
    private EntitySpawningUIController EntitySpawningController => UIManager.GetUIController<EntitySpawningUIController>();
    // Mythos: replace engine TileSpawningUIController with the variant-aware Mythos one.
    private MythosTileSpawningUIController TileSpawningController => UIManager.GetUIController<MythosTileSpawningUIController>();
    private DecalPlacerUIController DecalPlacerController => UIManager.GetUIController<DecalPlacerUIController>();

    private MenuButton? SandboxButton => UIManager.GetActiveUIWidgetOrNull<MenuBar.Widgets.GameTopMenuBar>()?.SandboxButton;

    public void OnStateEntered(GameplayState state)
    {
        DebugTools.Assert(_window == null);
        EnsureWindow();

        CheckSandboxVisibility();

        _input.SetInputCommand(ContentKeyFunctions.OpenEntitySpawnWindow,
            InputCmdHandler.FromDelegate(_ =>
            {
                if (!_admin.CanAdminPlace())
                    return;
                EntitySpawningController.ToggleWindow();
            }));
        _input.SetInputCommand(ContentKeyFunctions.OpenSandboxWindow,
            InputCmdHandler.FromDelegate(_ => ToggleWindow()));
        _input.SetInputCommand(ContentKeyFunctions.OpenTileSpawnWindow,
            InputCmdHandler.FromDelegate(_ =>
            {
                if (!_admin.CanAdminPlace())
                    return;
                TileSpawningController.ToggleWindow();
            }));
        _input.SetInputCommand(ContentKeyFunctions.OpenDecalSpawnWindow,
            InputCmdHandler.FromDelegate(_ =>
            {
                if (!_admin.CanAdminPlace())
                    return;
                DecalPlacerController.ToggleWindow();
            }));

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.EditorCopyObject, new PointerInputCmdHandler(Copy))
            .Register<SandboxSystem>();
    }

    public void UnloadButton()
    {
        if (SandboxButton == null)
        {
            return;
        }

        SandboxButton.OnPressed -= SandboxButtonPressed;
    }

    public void LoadButton()
    {
        if (SandboxButton == null)
        {
            return;
        }

        SandboxButton.OnPressed += SandboxButtonPressed;
    }

    private SandboxWindow EnsureWindow()
    {
        return UIManager.EnsureWindow(ref _window, w =>
        {
            // Pre-center the window without forcing it to the center every time.
            w.OpenCentered();
            w.Close();

            w.OnOpen += () => { SandboxButton!.Pressed = true; };
            w.OnClose += () => { SandboxButton!.Pressed = false; };

            w.AiOverlayButton.OnPressed += args =>
            {
                var player = _player.LocalEntity;

                if (player == null)
                    return;

                var pnent = EntityManager.GetNetEntity(player.Value);

                // Need NetworkedAddComponent but engine PR.
                if (args.Button.Pressed)
                    _console.ExecuteCommand($"addcomp {pnent.Id} StationAiOverlay");
                else
                    _console.ExecuteCommand($"rmcomp {pnent.Id} StationAiOverlay");
            };
            w.RespawnButton.OnPressed += _ => _sandbox.Respawn();
            w.SpawnTilesButton.OnPressed += _ => TileSpawningController.ToggleWindow();
            w.SpawnEntitiesButton.OnPressed += _ => EntitySpawningController.ToggleWindow();
            w.SpawnDecalsButton.OnPressed += _ => DecalPlacerController.ToggleWindow();
            w.GiveFullAccessButton.OnPressed += _ => _sandbox.GiveAdminAccess();
            w.GiveAghostButton.OnPressed += _ => _sandbox.GiveAGhost();
            w.ToggleLightButton.OnToggled += _ => _sandbox.ToggleLight();
            w.ToggleFovButton.OnToggled += _ => _sandbox.ToggleFov();
            w.ToggleShadowsButton.OnToggled += _ => _sandbox.ToggleShadows();
            w.SuicideButton.OnPressed += _ => _sandbox.Suicide();
            w.ToggleSubfloorButton.OnPressed += _ => _sandbox.ToggleSubFloor();
            w.ShowMarkersButton.OnPressed += _ => _sandbox.ShowMarkers();
            w.ShowBbButton.OnPressed += _ => _sandbox.ShowBb();
            w.ToggleThermalVisionButton.OnToggled += _ => _sandbox.ToggleThermalVision();
        });
    }

    private void CheckSandboxVisibility()
    {
        if (SandboxButton == null)
            return;

        SandboxButton.Visible = _sandbox.SandboxAllowed;
    }

    public void OnStateExited(GameplayState state)
    {
        if (_window != null)
        {
            _window.Close();
            _window = null;
        }

        CommandBinds.Unregister<SandboxSystem>();
    }

    public void OnSystemLoaded(SandboxSystem system)
    {
        system.SandboxDisabled += CloseAll;
        system.SandboxEnabled += CheckSandboxVisibility;
        system.SandboxDisabled += CheckSandboxVisibility;
    }

    public void OnSystemUnloaded(SandboxSystem system)
    {
        system.SandboxDisabled -= CloseAll;
        system.SandboxEnabled -= CheckSandboxVisibility;
        system.SandboxDisabled -= CheckSandboxVisibility;
    }

    private void SandboxButtonPressed(ButtonEventArgs args)
    {
        ToggleWindow();
    }

    private void CloseAll()
    {
        _window?.Close();
        EntitySpawningController.CloseWindow();
        TileSpawningController.CloseWindow();
    }

    private bool Copy(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        return _sandbox.Copy(session, coords, uid);
    }

    private void ToggleWindow()
    {
        var window = EnsureWindow();
        if (_sandbox.SandboxAllowed && !window.IsOpen)
        {
            UIManager.ClickSound();
            window.Open();
        }
        else
        {
            UIManager.ClickSound();
            window.Close();
        }
    }

    #region Buttons

    public void SetToggleSubfloors(bool value)
    {
        if (_window == null)
            return;

        _window.ToggleSubfloorButton.Pressed = value;
    }

    #endregion
}

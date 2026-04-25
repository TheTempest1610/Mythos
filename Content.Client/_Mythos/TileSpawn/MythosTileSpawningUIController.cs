using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared._Mythos.TileSpawn;
using Robust.Client.Graphics;
using Robust.Client.Placement;
using Robust.Client.Placement.Modes;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Enums;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client._Mythos.TileSpawn;

/// <summary>
/// Mythos: replaces RobustToolbox's <c>TileSpawningUIController</c>. Single-click
/// selection of (tile, variant): clicking on a specific variant cell of a tile's
/// icon strip both starts placement of that tile and sends a
/// <see cref="MsgSetTileVariantOverride"/> with the chosen variant. The selection
/// is highlighted in the list via <see cref="MythosTileItemList"/>.
/// </summary>
public sealed class MythosTileSpawningUIController : UIController
{
    [Dependency] private readonly IPlacementManager _placement = default!;
    [Dependency] private readonly IResourceCache _resources = default!;
    [Dependency] private readonly ITileDefinitionManager _tiles = default!;
    [Dependency] private readonly IClientNetManager _net = default!;

    private MythosTileSpawnWindow? _window;
    private bool _init;

    private readonly List<ITileDefinition> _shownTiles = new();
    private bool _clearingTileSelections;
    private bool _eraseTile;
    private bool _mirrorableTile;
    private bool _mirroredTile;

    private int _activeTileType;
    private byte _activeVariant;

    public override void Initialize()
    {
        DebugTools.Assert(_init == false);
        _init = true;
        _net.RegisterNetMessage<MsgSetTileVariantOverride>();
        _placement.PlacementChanged += ClearTileSelection;
        _placement.DirectionChanged += OnDirectionChanged;
        _placement.MirroredChanged += OnMirroredChanged;
    }

    public void ToggleWindow()
    {
        EnsureWindow();
        if (_window!.IsOpen)
        {
            _window.Close();
        }
        else
        {
            _window.Open();
            UpdateEntityDirectionLabel();
            UpdateMirroredButton();
            _window.SearchBar.GrabKeyboardFocus();
        }
    }

    public void CloseWindow()
    {
        if (_window == null || _window.Disposed)
            return;
        _window?.Close();
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;
        _window = UIManager.CreateWindow<MythosTileSpawnWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterLeft);
        _window.ClearButton.OnPressed += OnTileClearPressed;
        _window.SearchBar.OnTextChanged += OnTileSearchChanged;
        _window.TileList.OnItemVariantSelected += OnTileVariantSelected;
        _window.TileList.OnItemDeselected += OnTileItemDeselected;
        _window.EraseButton.Pressed = _eraseTile;
        _window.EraseButton.OnToggled += OnTileEraseToggled;
        _window.MirroredButton.Disabled = !_mirrorableTile;
        _window.RotationLabel.FontColorOverride = _mirrorableTile ? Color.White : Color.Gray;
        _window.MirroredButton.Pressed = _mirroredTile;
        _window.MirroredButton.OnToggled += OnTileMirroredToggled;
        BuildTileList();
    }

    private void StartTilePlacement(int tileType)
    {
        var newObjInfo = new PlacementInformation
        {
            PlacementOption = nameof(AlignTileAny),
            TileType = tileType,
            Range = 400,
            IsTile = true
        };
        _placement.BeginPlacing(newObjInfo);
        _activeTileType = tileType;
    }

    private void OnTileEraseToggled(ButtonToggledEventArgs args)
    {
        if (_window == null || _window.Disposed)
            return;
        _placement.Clear();
        if (args.Pressed)
        {
            _eraseTile = true;
            StartTilePlacement(0);
        }
        else
            _eraseTile = false;
        args.Button.Pressed = args.Pressed;
    }

    private void OnTileMirroredToggled(ButtonToggledEventArgs args)
    {
        if (_window == null || _window.Disposed)
            return;
        _placement.Mirrored = args.Pressed;
        _mirroredTile = _placement.Mirrored;
        args.Button.Pressed = args.Pressed;
    }

    private void ClearTileSelection(object? sender, EventArgs e)
    {
        if (_window == null || _window.Disposed) return;
        _clearingTileSelections = true;
        _window.TileList.ClearSelected();
        _clearingTileSelections = false;
        _window.EraseButton.Pressed = false;
        _window.MirroredButton.Pressed = _placement.Mirrored;

        // Clear server-side override so the next placement defaults to variant 0.
        if (_activeVariant != 0)
            SendOverride(_activeTileType, 0);
        _activeTileType = 0;
        _activeVariant = 0;
    }

    private void OnTileClearPressed(ButtonEventArgs args)
    {
        if (_window == null || _window.Disposed) return;
        _window.TileList.ClearSelected();
        _placement.Clear();
        _window.SearchBar.Clear();
        BuildTileList(string.Empty);
        _window.ClearButton.Disabled = true;
    }

    private void OnTileSearchChanged(LineEdit.LineEditEventArgs args)
    {
        if (_window == null || _window.Disposed) return;
        _window.TileList.ClearSelected();
        _placement.Clear();
        BuildTileList(args.Text);
        _window.ClearButton.Disabled = string.IsNullOrEmpty(args.Text);
    }

    private void OnTileVariantSelected(int itemIndex, byte variant)
    {
        if (itemIndex < 0 || itemIndex >= _shownTiles.Count)
            return;
        var def = _shownTiles[itemIndex];
        StartTilePlacement(def.TileId);
        UpdateMirroredButton();

        _activeVariant = variant;
        SendOverride(def.TileId, variant);
    }

    private void OnTileItemDeselected(ItemList.ItemListDeselectedEventArgs args)
    {
        if (_clearingTileSelections)
            return;
        _placement.Clear();
    }

    private void OnDirectionChanged(object? sender, EventArgs e) => UpdateEntityDirectionLabel();

    private void UpdateEntityDirectionLabel()
    {
        if (_window == null || _window.Disposed) return;
        _window.RotationLabel.Text = _placement.Direction.ToString();
    }

    private void OnMirroredChanged(object? sender, EventArgs e) => UpdateMirroredButton();

    private void UpdateMirroredButton()
    {
        if (_window == null || _window.Disposed) return;

        if (_placement.CurrentPermission != null && _placement.CurrentPermission.IsTile)
        {
            var allowed = _tiles[_placement.CurrentPermission.TileType].AllowRotationMirror;
            _mirrorableTile = allowed;
            _window.MirroredButton.Disabled = !_mirrorableTile;
            _window.RotationLabel.FontColorOverride = _mirrorableTile ? Color.White : Color.Gray;
        }

        _mirroredTile = _placement.Mirrored;
        _window.MirroredButton.Pressed = _mirroredTile;
    }

    private void BuildTileList(string? searchStr = null)
    {
        if (_window == null || _window.Disposed) return;

        _window.TileList.ClearTileItems();

        IEnumerable<ITileDefinition> tileDefs = _tiles.Where(def => !def.EditorHidden);

        if (!string.IsNullOrEmpty(searchStr))
        {
            tileDefs = tileDefs.Where(s =>
                Loc.GetString(s.Name).Contains(searchStr, StringComparison.CurrentCultureIgnoreCase) ||
                s.ID.Contains(searchStr, StringComparison.OrdinalIgnoreCase));
        }

        tileDefs = tileDefs.OrderBy(d => Loc.GetString(d.Name));

        _shownTiles.Clear();
        _shownTiles.AddRange(tileDefs);

        foreach (var entry in _shownTiles)
        {
            Texture? texture = null;
            var path = entry.Sprite?.ToString();
            if (path != null)
                texture = _resources.GetResource<TextureResource>(path);
            _window.TileList.AddTileItem(Loc.GetString(entry.Name), texture, entry.Variants);
        }
    }

    private void SendOverride(int tileType, byte variant)
    {
        if (!_net.IsConnected)
            return;
        var msg = new MsgSetTileVariantOverride
        {
            TileType = tileType,
            Variant = variant,
        };
        _net.ClientSendMessage(msg);
    }
}

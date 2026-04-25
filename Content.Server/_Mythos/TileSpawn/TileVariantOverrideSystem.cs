using System.Collections.Generic;
using Content.Server.Administration.Managers;
using Content.Shared._Mythos.TileSpawn;
using Content.Shared.Administration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Placement;
using Robust.Shared.Player;

namespace Content.Server._Mythos.TileSpawn;

/// <summary>
/// Mythos: lets the tile-spawn UI pick a specific variant by clicking on the variant
/// strip. The engine's placement flow always writes variant 0; this system intercepts
/// the resulting PlacementTileEvent and re-sets the tile with the player's chosen
/// variant. Two SetTile calls per placement, but only for users with an active
/// override and Mapping admin flag.
/// </summary>
public sealed class TileVariantOverrideSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    // For each user, the override they last requested. (TileType, Variant) so the
    // override only kicks in when the placed tile matches what they picked a variant for.
    private readonly Dictionary<NetUserId, (int TileType, byte Variant)> _overrides = new();

    public override void Initialize()
    {
        base.Initialize();
        _net.RegisterNetMessage<MsgSetTileVariantOverride>(OnSetOverride);
        SubscribeLocalEvent<PlacementTileEvent>(OnTilePlaced);
    }

    private void OnSetOverride(MsgSetTileVariantOverride msg)
    {
        var userId = msg.MsgChannel.UserId;

        if (!_player.TryGetSessionById(userId, out var session))
            return;

        // Same gate as the in-engine sandbox keybind: only mapping admins can drive this.
        if (!_admin.HasAdminFlag(session, AdminFlags.Mapping))
            return;

        if (msg.Variant == 0)
        {
            // Variant 0 matches the default placement behaviour; no point holding state.
            _overrides.Remove(userId);
            return;
        }

        _overrides[userId] = (msg.TileType, msg.Variant);
    }

    private void OnTilePlaced(PlacementTileEvent ev)
    {
        if (ev.PlacerNetUserId is not { } userId)
            return;
        if (!_overrides.TryGetValue(userId, out var pick))
            return;
        if (pick.TileType != ev.TileType)
            return;

        var coords = ev.Coordinates;
        if (!TryComp<MapGridComponent>(coords.EntityId, out var grid))
            return;

        var mapPos = _transform.ToMapCoordinates(coords).Position;
        var tilePos = _maps.WorldToTile(coords.EntityId, grid, mapPos);
        var existing = _maps.GetTileRef(coords.EntityId, grid, tilePos).Tile;

        // Guard against a race: tile may have been erased/replaced between SetTile and our handler.
        if (existing.TypeId != ev.TileType)
            return;

        _maps.SetTile(coords.EntityId, grid, tilePos,
            new Tile(ev.TileType, existing.Flags, pick.Variant, existing.RotationMirroring));
    }
}

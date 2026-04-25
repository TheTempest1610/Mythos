using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Mythos.TileSpawn;

/// <summary>
/// Mythos: client → server.
/// Tells the server "next time I place tile <see cref="TileType"/>, use <see cref="Variant"/>".
/// The override is consumed by <c>TileVariantOverrideSystem</c> via PlacementTileEvent.
/// Sending Variant=0 is the default (matches engine behaviour) and effectively clears
/// any prior override for that TileType.
/// </summary>
public sealed class MsgSetTileVariantOverride : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public int TileType;
    public byte Variant;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        TileType = buffer.ReadInt32();
        Variant = buffer.ReadByte();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(TileType);
        buffer.Write(Variant);
    }
}

using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

public sealed class MsgServerTickTime : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public float AverageTickMs;
    public float StdDevMs;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        AverageTickMs = buffer.ReadFloat();
        StdDevMs = buffer.ReadFloat();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(AverageTickMs);
        buffer.Write(StdDevMs);
    }

    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.Unreliable;
}

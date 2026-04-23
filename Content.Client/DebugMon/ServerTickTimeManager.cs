using Content.Shared.Administration;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.Client.DebugMon;

/// <summary>
/// Receives server tick time broadcasts and exposes the latest values
/// for the F3 debug overlay to display.
/// </summary>
public sealed class ServerTickTimeManager
{
    [Dependency] private readonly IClientNetManager _net = default!;

    public float AverageTickMs { get; private set; }
    public float StdDevMs { get; private set; }
    public bool HasData { get; private set; }

    public void Initialize()
    {
        _net.RegisterNetMessage<MsgServerTickTime>(OnMessage);
    }

    private void OnMessage(MsgServerTickTime msg)
    {
        AverageTickMs = msg.AverageTickMs;
        StdDevMs = msg.StdDevMs;
        HasData = true;
    }
}

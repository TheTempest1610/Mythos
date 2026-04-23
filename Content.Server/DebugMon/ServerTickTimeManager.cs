using System;
using Content.Shared.Administration;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server.DebugMon;

/// <summary>
/// Broadcasts the server's average game loop frame time to all clients on an interval
/// so it can be displayed in the client F3 debug overlay.
/// </summary>
public sealed class ServerTickTimeManager
{
    private static readonly TimeSpan BroadcastInterval = TimeSpan.FromSeconds(1);

    [Dependency] private readonly IServerNetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private TimeSpan _nextBroadcast;

    public void Initialize()
    {
        _net.RegisterNetMessage<MsgServerTickTime>();
    }

    public void Update()
    {
        if (_timing.RealTime < _nextBroadcast)
            return;

        _nextBroadcast = _timing.RealTime + BroadcastInterval;

        var msg = new MsgServerTickTime
        {
            AverageTickMs = (float)_timing.RealFrameTimeAvg.TotalMilliseconds,
            StdDevMs = (float)_timing.RealFrameTimeStdDev.TotalMilliseconds,
        };

        _net.ServerSendToAll(msg);
    }
}

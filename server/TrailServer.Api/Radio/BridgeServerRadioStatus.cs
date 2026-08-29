using TrailServer.RadioBridge;

namespace TrailServer.Api.Radio;

public sealed class BridgeServerRadioStatus(IRadioBridgeState bridgeState) : IServerRadioStatus
{
    public ServerRadioStatus GetStatus()
    {
        var snapshot = bridgeState.GetSnapshot();
        return snapshot.Phase == RadioBridgePhase.SessionReady
            ? new("connected", snapshot.Reason)
            : new("unavailable", snapshot.Reason);
    }
}

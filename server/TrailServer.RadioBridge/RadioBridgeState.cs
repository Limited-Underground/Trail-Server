namespace TrailServer.RadioBridge;

public enum RadioBridgePhase
{
    Disabled,
    Connecting,
    Handshaking,
    SessionReady,
    Unavailable,
}

public sealed record RadioBridgeSnapshot(RadioBridgePhase Phase, string Reason)
{
    public static RadioBridgeSnapshot Disabled { get; } = new(RadioBridgePhase.Disabled, "not-configured");
}

public interface IRadioBridgeState
{
    RadioBridgeSnapshot GetSnapshot();
}

public sealed class RadioBridgeState : IRadioBridgeState
{
    private RadioBridgeSnapshot snapshot = RadioBridgeSnapshot.Disabled;

    public RadioBridgeSnapshot GetSnapshot() => Volatile.Read(ref snapshot);

    public void Set(RadioBridgePhase phase, string reason) =>
        Interlocked.Exchange(ref snapshot, new RadioBridgeSnapshot(phase, reason));
}

using System.ComponentModel.DataAnnotations;

namespace TrailServer.RadioBridge;

public sealed class RadioBridgeOptions
{
    public const string SectionName = "TrailServer:RadioBridge";

    public bool Enabled { get; init; }

    [Range(1, 30)]
    public int HelloTimeoutSeconds { get; init; } = 5;

    [Range(1, 300)]
    public int HeartbeatTimeoutSeconds { get; init; } = 30;

    [Range(1, 60)]
    public int ReconnectDelaySeconds { get; init; } = 5;
}

namespace TrailServer.Api.Radio;

public sealed class UnavailableServerRadioStatus : IServerRadioStatus
{
    private static readonly ServerRadioStatus Status = new(
        Availability: "unavailable",
        Reason: "not-configured");

    public ServerRadioStatus GetStatus() => Status;
}

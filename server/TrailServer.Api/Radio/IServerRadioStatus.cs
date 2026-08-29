namespace TrailServer.Api.Radio;

public interface IServerRadioStatus
{
    ServerRadioStatus GetStatus();
}

public sealed record ServerRadioStatus(string Availability, string Reason);

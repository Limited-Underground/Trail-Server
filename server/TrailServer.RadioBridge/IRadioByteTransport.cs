namespace TrailServer.RadioBridge;

public interface IRadioByteTransport
{
    ValueTask<Stream> OpenAsync(CancellationToken cancellationToken);
}

public sealed class DisabledRadioByteTransport : IRadioByteTransport
{
    public ValueTask<Stream> OpenAsync(CancellationToken cancellationToken) =>
        ValueTask.FromException<Stream>(new RadioTransportUnavailableException());
}

public sealed class RadioTransportUnavailableException : Exception;

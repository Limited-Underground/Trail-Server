using System.IO.Ports;
using Microsoft.Extensions.Options;

namespace TrailServer.RadioBridge;

public sealed class ConfiguredRadioByteTransport : IRadioByteTransport
{
    private readonly RadioBridgeOptions configuration;
    private readonly ISerialConnectionFactory serialConnections;

    public ConfiguredRadioByteTransport(IOptions<RadioBridgeOptions> options)
        : this(options.Value, new SystemSerialConnectionFactory())
    {
    }

    internal ConfiguredRadioByteTransport(
        RadioBridgeOptions configuration,
        ISerialConnectionFactory serialConnections)
    {
        this.configuration = configuration;
        this.serialConnections = serialConnections;
    }

    public async ValueTask<Stream> OpenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!configuration.Enabled ||
            !string.Equals(configuration.Transport, "serial", StringComparison.OrdinalIgnoreCase) ||
            !RadioSerialDevicePath.IsStable(configuration.SerialDevicePath))
            throw new RadioTransportUnavailableException();

        ISerialConnection? connection = null;
        try
        {
            connection = serialConnections.Create(
                configuration.SerialDevicePath!,
                configuration.SerialBaudRate);
            var stream = connection.Open();
            cancellationToken.ThrowIfCancellationRequested();
            return new OwnedSerialStream(stream, connection);
        }
        catch (OperationCanceledException)
        {
            if (connection is not null) await connection.DisposeAsync();
            throw;
        }
        catch
        {
            if (connection is not null) await connection.DisposeAsync();
            throw new RadioTransportUnavailableException();
        }
    }
}

internal interface ISerialConnectionFactory
{
    ISerialConnection Create(string devicePath, int baudRate);
}

internal interface ISerialConnection : IDisposable, IAsyncDisposable
{
    Stream Open();
}

internal sealed class SystemSerialConnectionFactory : ISerialConnectionFactory
{
    public ISerialConnection Create(string devicePath, int baudRate) =>
        new SystemSerialConnection(devicePath, baudRate);
}

internal sealed class SystemSerialConnection(string devicePath, int baudRate) : ISerialConnection
{
    private readonly SerialPort port = new(devicePath, baudRate, Parity.None, 8, StopBits.One)
    {
        Handshake = Handshake.None,
        DtrEnable = false,
        RtsEnable = false,
        ReadTimeout = SerialPort.InfiniteTimeout,
        WriteTimeout = SerialPort.InfiniteTimeout,
    };

    public Stream Open()
    {
        port.Open();
        return port.BaseStream;
    }

    public void Dispose() => port.Dispose();

    public ValueTask DisposeAsync()
    {
        port.Dispose();
        return ValueTask.CompletedTask;
    }
}

internal sealed class OwnedSerialStream(Stream stream, ISerialConnection connection) : Stream
{
    public override bool CanRead => stream.CanRead;
    public override bool CanSeek => stream.CanSeek;
    public override bool CanWrite => stream.CanWrite;
    public override long Length => stream.Length;
    public override long Position { get => stream.Position; set => stream.Position = value; }
    public override void Flush() => stream.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => stream.FlushAsync(cancellationToken);
    public override int Read(byte[] buffer, int offset, int count) => stream.Read(buffer, offset, count);
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        stream.ReadAsync(buffer, cancellationToken);
    public override long Seek(long offset, SeekOrigin origin) => stream.Seek(offset, origin);
    public override void SetLength(long value) => stream.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => stream.Write(buffer, offset, count);
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
        stream.WriteAsync(buffer, cancellationToken);

    protected override void Dispose(bool disposing)
    {
        if (disposing) connection.Dispose();
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await connection.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}

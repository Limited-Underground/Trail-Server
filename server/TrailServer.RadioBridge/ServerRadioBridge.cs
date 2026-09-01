using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TrailServer.RadioContract;

namespace TrailServer.RadioBridge;

public sealed class ServerRadioBridge(
    IRadioByteTransport transport,
    RadioBridgeState state,
    IOptions<RadioBridgeOptions> options,
    ILogger<ServerRadioBridge> logger) : BackgroundService
{
    private readonly RadioBridgeOptions configuration = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.Enabled)
        {
            state.Set(RadioBridgePhase.Disabled, "not-configured");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                state.Set(RadioBridgePhase.Connecting, "opening-transport");
                await using var stream = await transport.OpenAsync(stoppingToken);
                var reader = new RadioBridgeFrameReader(stream);
                state.Set(RadioBridgePhase.Handshaking, "awaiting-hello");
                var session = await RadioBridgeSession.HandshakeAsync(
                    stream,
                    reader,
                    TimeSpan.FromSeconds(configuration.HelloTimeoutSeconds),
                    stoppingToken);
                state.Set(RadioBridgePhase.SessionReady, "session-ready-no-persistence");
                await RadioBridgeSession.MonitorAsync(
                    stream,
                    reader,
                    session,
                    TimeSpan.FromSeconds(configuration.HeartbeatTimeoutSeconds),
                    stoppingToken);
                state.Set(RadioBridgePhase.Unavailable, "transport-eof");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (RadioBridgeProtocolException exception)
            {
                state.Set(RadioBridgePhase.Unavailable, exception.Code);
                logger.LogWarning("Radio bridge session ended; code={Code}", PrivacySafeError.NormalizeCode(exception.Code));
            }
            catch (RadioTransportUnavailableException)
            {
                state.Set(RadioBridgePhase.Unavailable, "transport_unavailable");
                logger.LogWarning("Radio bridge session ended; code={Code}", "transport_unavailable");
            }
            catch (Exception)
            {
                state.Set(RadioBridgePhase.Unavailable, "radio_unavailable");
                logger.LogWarning("Radio bridge session ended; code={Code}", "radio_unavailable");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(configuration.ReconnectDelaySeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        state.Set(RadioBridgePhase.Unavailable, "service-stopped");
    }
}

public sealed class RadioBridgeProtocolException(string code) : Exception
{
    public string Code { get; } = code;
}

public static class RadioBridgeSession
{
    private static readonly HashSet<ulong> HostCapabilities = [1, 2];

    public static async Task<LusrSession> HandshakeAsync(Stream stream, TimeSpan timeout, CancellationToken cancellationToken)
        => await HandshakeAsync(stream, new RadioBridgeFrameReader(stream), timeout, cancellationToken);

    public static async Task<LusrSession> HandshakeAsync(
        Stream stream,
        RadioBridgeFrameReader reader,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        var frame = await reader.ReadAsync(timeoutSource.Token);
        if (frame.ProtocolMajor != 1 || !LusrMessageCodec.TryDecodeHello(frame, out var hello) || hello!.ProtocolMajor != 1)
        {
            throw new RadioBridgeProtocolException("protocol_version");
        }
        if (hello.MaximumDecodedRecordBytes < 12 || hello.MaximumOpaquePayloadBytes == 0)
        {
            throw new RadioBridgeProtocolException("oversize");
        }

        var session = new LusrSession(
            Guid.NewGuid(),
            hello.RadioId,
            hello.BootId,
            Math.Min((byte)0, hello.MaximumMinor),
            Math.Min((ushort)4140, hello.MaximumDecodedRecordBytes),
            Math.Min((ushort)4096, hello.MaximumOpaquePayloadBytes),
            hello.Capabilities.Where(HostCapabilities.Contains).ToHashSet());
        var ack = new RadioFrame(1, session.ProtocolMinor, (byte)RadioMessageType.HelloAck,
            FrameCodec.MustUnderstandFlag, LusrMessageCodec.EncodeHelloAck(session));
        await stream.WriteAsync(FrameCodec.Encode(ack), cancellationToken);
        await stream.FlushAsync(cancellationToken);
        return session;
    }

    public static async Task MonitorAsync(Stream stream, LusrSession session, TimeSpan timeout, CancellationToken cancellationToken)
        => await MonitorAsync(stream, new RadioBridgeFrameReader(stream), session, timeout, cancellationToken);

    public static async Task MonitorAsync(
        Stream stream,
        RadioBridgeFrameReader reader,
        LusrSession session,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);
            RadioFrame frame;
            try
            {
                frame = await reader.ReadAsync(timeoutSource.Token);
            }
            catch (EndOfStreamException)
            {
                return;
            }

            if (frame.ProtocolMajor != 1 || frame.ProtocolMinor != session.ProtocolMinor)
                throw new RadioBridgeProtocolException("protocol_version");
            if (MessagePolicy.MustReject(frame)) throw new RadioBridgeProtocolException("unsupported");
            if (!MessagePolicy.IsKnown(frame.MessageType)) continue;
            if (!MessagePayloadValidator.Validate(frame).Success ||
                !MessagePayloadValidator.TryGetSessionId(frame, out var frameSession) || frameSession != session.SessionId)
            {
                throw new RadioBridgeProtocolException("invalid_state");
            }

            if (frame.MessageType is (byte)RadioMessageType.Heartbeat or (byte)RadioMessageType.Status) continue;
            if (frame.MessageType == (byte)RadioMessageType.RxPacket)
                throw new RadioBridgeProtocolException("radio_unavailable");
            throw new RadioBridgeProtocolException("invalid_state");
        }
    }

}

public sealed class RadioBridgeFrameReader(Stream stream)
{
    private readonly FrameStreamDecoder decoder = new();
    private readonly Queue<RadioFrame> pending = new();

    public async Task<RadioFrame> ReadAsync(CancellationToken cancellationToken)
    {
        if (pending.TryDequeue(out var queued)) return queued;
        var buffer = new byte[256];
        while (true)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken);
            if (read == 0) throw new EndOfStreamException();
            foreach (var result in decoder.Feed(buffer.AsSpan(0, read)))
            {
                if (result.Success) pending.Enqueue(result.Frame!);
            }
            if (pending.TryDequeue(out var frame)) return frame;
        }
    }
}

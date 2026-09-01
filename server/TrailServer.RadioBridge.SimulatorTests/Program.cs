using System.Formats.Cbor;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TrailServer.RadioBridge;
using TrailServer.RadioContract;

var tests = new (string Name, Func<Task> Run)[]
{
    ("disabled worker never opens transport", DisabledWorker),
    ("serial options require an explicit stable Linux device path", SerialOptionsValidation),
    ("configured serial transport uses exact path and baud", ConfiguredSerialTransport),
    ("cancelled serial open performs no device access", CancelledSerialOpen),
    ("failed serial open is redacted and disposed", FailedSerialOpen),
    ("fragmented HELLO resynchronizes and negotiates", FragmentedHandshake),
    ("coalesced frames remain connection-scoped", CoalescedFrames),
    ("major mismatch fails closed", MajorMismatch),
    ("impossible limits fail closed", ImpossibleLimits),
    ("current-session heartbeat reaches clean EOF", CurrentSessionHeartbeat),
    ("post-handshake version mismatch fails closed", PostHandshakeVersionMismatch),
    ("stale session is rejected", StaleSession),
    ("unknown mandatory frame terminates", UnknownMandatory),
    ("RX packet is never acknowledged without persistence", ReceiveWithoutPersistence),
    ("enabled worker reconnects fresh and disposes streams", EnabledWorkerLifecycle),
};

var failures = 0;
foreach (var test in tests)
{
    try
    {
        await test.Run();
        Console.WriteLine($"PASS: {test.Name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.Error.WriteLine($"FAIL: {test.Name}: {exception.Message}");
    }
}

Console.WriteLine();
Console.WriteLine($"RADIO_BRIDGE_SIMULATOR_RESULT={(failures == 0 ? "PASS" : "FAIL")}");
return failures == 0 ? 0 : 1;

static async Task DisabledWorker()
{
    var transport = new CountingTransport();
    var state = new RadioBridgeState();
    var worker = new ServerRadioBridge(
        transport,
        state,
        Options.Create(new RadioBridgeOptions { Enabled = false }),
        NullLogger<ServerRadioBridge>.Instance);
    await worker.StartAsync(CancellationToken.None);
    await worker.StopAsync(CancellationToken.None);
    AssertEqual(0, transport.OpenCount, "Disabled worker opened transport");
    AssertEqual(RadioBridgePhase.Disabled, state.GetSnapshot().Phase, "Disabled worker changed authority");
    AssertEqual("not-configured", state.GetSnapshot().Reason, "Disabled reason changed");
}

static Task SerialOptionsValidation()
{
    var validator = new RadioBridgeOptionsValidator();
    Assert(validator.Validate(null, new RadioBridgeOptions()).Succeeded, "Disabled defaults did not validate");

    foreach (var invalid in new[]
    {
        new RadioBridgeOptions { Enabled = true, Transport = "disabled" },
        new RadioBridgeOptions { Enabled = true, Transport = "serial" },
        new RadioBridgeOptions { Enabled = true, Transport = " serial ", SerialDevicePath = "/dev/serial/by-id/test" },
        new RadioBridgeOptions { Enabled = true, Transport = "serial", SerialDevicePath = "/dev/ttyUSB0" },
        new RadioBridgeOptions { Enabled = true, Transport = "serial", SerialDevicePath = "/dev/serial/by-id/../ttyUSB0" },
        new RadioBridgeOptions { Enabled = true, Transport = "serial", SerialDevicePath = "COM3" },
        new RadioBridgeOptions { Transport = "disabled", SerialDevicePath = "/dev/serial/by-id/test" },
    })
        Assert(!validator.Validate(null, invalid).Succeeded, "Unsafe serial configuration validated");

    var valid = new RadioBridgeOptions
    {
        Enabled = true,
        Transport = "serial",
        SerialDevicePath = "/dev/serial/by-id/usb-Limited_Underground_Trail_Radio-if00",
        SerialBaudRate = 115_200,
    };
    Assert(validator.Validate(null, valid).Succeeded, "Explicit stable serial configuration did not validate");
    var invalidBaud = new RadioBridgeOptions
    {
        Enabled = true,
        Transport = "serial",
        SerialDevicePath = valid.SerialDevicePath,
        SerialBaudRate = 0,
    };
    var annotationResults = new List<ValidationResult>();
    Assert(!Validator.TryValidateObject(invalidBaud, new ValidationContext(invalidBaud), annotationResults, true),
        "Out-of-range baud rate passed data-annotation validation");
    return Task.CompletedTask;
}

static async Task ConfiguredSerialTransport()
{
    const string path = "/dev/serial/by-id/usb-Limited_Underground_Trail_Radio-if00";
    var factory = new RecordingSerialConnectionFactory();
    var transport = new ConfiguredRadioByteTransport(new RadioBridgeOptions
    {
        Enabled = true,
        Transport = "serial",
        SerialDevicePath = path,
        SerialBaudRate = 230_400,
    }, factory);

    await using (var stream = await transport.OpenAsync(CancellationToken.None))
    {
        Assert(stream.CanRead && stream.CanWrite, "Opened serial stream is not duplex");
    }

    AssertEqual(path, factory.DevicePath!, "Configured device path changed");
    AssertEqual(230_400, factory.BaudRate, "Configured baud rate changed");
    Assert(factory.Connection!.Disposed, "Disposing the stream did not dispose the serial owner");
}

static async Task CancelledSerialOpen()
{
    var factory = new RecordingSerialConnectionFactory();
    var transport = ValidConfiguredTransport(factory);
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();
    await AssertThrows<OperationCanceledException>(() => transport.OpenAsync(cancellation.Token).AsTask());
    AssertEqual(0, factory.CreateCount, "Cancelled open accessed the serial device");
}

static async Task FailedSerialOpen()
{
    const string privatePath = "/dev/serial/by-id/private-device-identity";
    var factory = new RecordingSerialConnectionFactory(openFailure: new IOException($"denied: {privatePath}"));
    var transport = new ConfiguredRadioByteTransport(new RadioBridgeOptions
    {
        Enabled = true,
        Transport = "serial",
        SerialDevicePath = privatePath,
    }, factory);

    var failure = await AssertThrows<RadioTransportUnavailableException>(() =>
        transport.OpenAsync(CancellationToken.None).AsTask());
    Assert(!failure.ToString().Contains(privatePath, StringComparison.Ordinal), "Transport failure exposed the device path");
    Assert(factory.Connection!.Disposed, "Failed serial open did not dispose the connection");
}

static ConfiguredRadioByteTransport ValidConfiguredTransport(ISerialConnectionFactory factory) => new(
    new RadioBridgeOptions
    {
        Enabled = true,
        Transport = "serial",
        SerialDevicePath = "/dev/serial/by-id/test-radio",
    },
    factory);

static async Task FragmentedHandshake()
{
    var hello = ValidHello();
    var valid = FrameCodec.Encode(new(1, 0, (byte)RadioMessageType.Hello, FrameCodec.MustUnderstandFlag,
        LusrMessageCodec.EncodeHello(hello)));
    var corrupt = valid.ToArray();
    corrupt[^2] ^= 0x20;
    await using var stream = new ScriptedDuplexStream(corrupt.Concat(valid).ToArray(), 3);
    var session = await RadioBridgeSession.HandshakeAsync(stream, TimeSpan.FromSeconds(2), CancellationToken.None);
    AssertEqual(hello.RadioId, session.RadioId, "Radio identity changed");
    AssertEqual(hello.BootId, session.BootId, "Boot identity changed");
    AssertEqual((ushort)1024, session.MaximumOpaquePayloadBytes, "Payload limit was not negotiated");
    Assert(session.Capabilities.SetEquals([1]), "Capabilities were not intersected");

    var ack = FrameCodec.Decode(stream.WrittenBytes);
    Assert(ack.Success, $"HELLO_ACK framing failed: {ack.Error}");
    AssertEqual((byte)RadioMessageType.HelloAck, ack.Frame!.MessageType, "Handshake wrote the wrong response");
    Assert(MessagePayloadValidator.Validate(ack.Frame).Success, "HELLO_ACK payload is invalid");
    AssertEqual(session.SessionId, ReadSessionId(ack.Frame.Payload), "HELLO_ACK session differs from worker state");
}

static async Task MajorMismatch()
{
    var hello = ValidHello() with { ProtocolMajor = 2 };
    var frame = FrameCodec.Encode(new(2, 0, (byte)RadioMessageType.Hello, FrameCodec.MustUnderstandFlag,
        LusrMessageCodec.EncodeHello(hello)));
    await using var stream = new ScriptedDuplexStream(frame);
    await AssertProtocolFailure("protocol_version", () =>
        RadioBridgeSession.HandshakeAsync(stream, TimeSpan.FromSeconds(2), CancellationToken.None));
    AssertEqual(0, stream.WrittenBytes.Length, "Major mismatch produced a success response");
}

static async Task CoalescedFrames()
{
    var hello = ValidHello();
    var helloFrame = FrameCodec.Encode(new(1, 0, (byte)RadioMessageType.Hello, FrameCodec.MustUnderstandFlag,
        LusrMessageCodec.EncodeHello(hello)));
    var mandatoryUnknown = FrameCodec.Encode(new(1, 0, 0x55, FrameCodec.MustUnderstandFlag, [0xA0]));
    await using var stream = new ScriptedDuplexStream(helloFrame.Concat(mandatoryUnknown).ToArray());
    var reader = new RadioBridgeFrameReader(stream);
    var session = await RadioBridgeSession.HandshakeAsync(stream, reader, TimeSpan.FromSeconds(2), CancellationToken.None);
    await AssertProtocolFailure("unsupported", () =>
        RadioBridgeSession.MonitorAsync(stream, reader, session, TimeSpan.FromSeconds(2), CancellationToken.None));
}

static async Task ImpossibleLimits()
{
    foreach (var hello in new[]
    {
        ValidHello() with { MaximumDecodedRecordBytes = 11 },
        ValidHello() with { MaximumOpaquePayloadBytes = 0 },
    })
    {
        var frame = FrameCodec.Encode(new(1, 0, (byte)RadioMessageType.Hello, FrameCodec.MustUnderstandFlag,
            LusrMessageCodec.EncodeHello(hello)));
        await using var stream = new ScriptedDuplexStream(frame);
        await AssertProtocolFailure("oversize", () =>
            RadioBridgeSession.HandshakeAsync(stream, TimeSpan.FromSeconds(2), CancellationToken.None));
    }
}

static async Task CurrentSessionHeartbeat()
{
    var session = TestSession();
    var heartbeat = PostHandshakeFrame(RadioMessageType.Heartbeat, session.SessionId, writer =>
        WriteUnsigned(writer, 2, 1));
    await using var stream = new ScriptedDuplexStream(FrameCodec.Encode(heartbeat));
    await RadioBridgeSession.MonitorAsync(stream, session, TimeSpan.FromSeconds(2), CancellationToken.None);
    AssertEqual(0, stream.WrittenBytes.Length, "Heartbeat produced an invented acknowledgement");
}

static async Task StaleSession()
{
    var session = TestSession();
    var heartbeat = PostHandshakeFrame(RadioMessageType.Heartbeat, Guid.NewGuid(), writer =>
        WriteUnsigned(writer, 2, 1));
    await using var stream = new ScriptedDuplexStream(FrameCodec.Encode(heartbeat));
    await AssertProtocolFailure("invalid_state", () =>
        RadioBridgeSession.MonitorAsync(stream, session, TimeSpan.FromSeconds(2), CancellationToken.None));
}

static async Task PostHandshakeVersionMismatch()
{
    var session = TestSession();
    var heartbeat = PostHandshakeFrame(RadioMessageType.Heartbeat, session.SessionId, writer =>
        WriteUnsigned(writer, 2, 1)) with { ProtocolMajor = 2 };
    await using var stream = new ScriptedDuplexStream(FrameCodec.Encode(heartbeat));
    await AssertProtocolFailure("protocol_version", () =>
        RadioBridgeSession.MonitorAsync(stream, session, TimeSpan.FromSeconds(2), CancellationToken.None));
}

static async Task UnknownMandatory()
{
    var session = TestSession();
    var unknown = new RadioFrame(1, 0, 0x55, FrameCodec.MustUnderstandFlag, [0xA0]);
    await using var stream = new ScriptedDuplexStream(FrameCodec.Encode(unknown));
    await AssertProtocolFailure("unsupported", () =>
        RadioBridgeSession.MonitorAsync(stream, session, TimeSpan.FromSeconds(2), CancellationToken.None));
}

static async Task ReceiveWithoutPersistence()
{
    var session = TestSession();
    var receive = PostHandshakeFrame(RadioMessageType.RxPacket, session.SessionId, writer =>
    {
        WriteUuid(writer, 2, session.BootId);
        WriteUnsigned(writer, 3, 1);
        writer.WriteInt32(4);
        writer.WriteByteString([0xAA, 0xBB]);
    });
    await using var stream = new ScriptedDuplexStream(FrameCodec.Encode(receive));
    await AssertProtocolFailure("radio_unavailable", () =>
        RadioBridgeSession.MonitorAsync(stream, session, TimeSpan.FromSeconds(2), CancellationToken.None));
    AssertEqual(0, stream.WrittenBytes.Length, "RX_PACKET was acknowledged without durable storage");
}

static async Task EnabledWorkerLifecycle()
{
    var first = HandshakeStream();
    var second = HandshakeStream();
    var transport = new LifecycleTransport(first, second);
    var state = new RadioBridgeState();
    var worker = new ServerRadioBridge(
        transport,
        state,
        Options.Create(new RadioBridgeOptions
        {
            Enabled = true,
            HelloTimeoutSeconds = 2,
            HeartbeatTimeoutSeconds = 2,
            ReconnectDelaySeconds = 1,
        }),
        NullLogger<ServerRadioBridge>.Instance);

    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    await worker.StartAsync(timeout.Token);
    await WaitUntil(() => second.Disposed, timeout.Token);
    await worker.StopAsync(timeout.Token);
    Assert(first.Disposed && second.Disposed, "Reconnect did not dispose both streams");
    Assert(transport.OpenCount >= 2, "EOF did not cause bounded reconnect");
    var firstSession = ReadSessionId(FrameCodec.Decode(first.WrittenBytes).Frame!.Payload);
    var secondSession = ReadSessionId(FrameCodec.Decode(second.WrittenBytes).Frame!.Payload);
    Assert(firstSession != secondSession, "Reconnect reused the prior session identity");
}

static ScriptedDuplexStream HandshakeStream()
{
    var hello = ValidHello();
    var frame = FrameCodec.Encode(new(1, 0, (byte)RadioMessageType.Hello, FrameCodec.MustUnderstandFlag,
        LusrMessageCodec.EncodeHello(hello)));
    return new ScriptedDuplexStream(frame);
}

static async Task WaitUntil(Func<bool> condition, CancellationToken cancellationToken)
{
    while (!condition()) await Task.Delay(20, cancellationToken);
}

static LusrHello ValidHello() => new(
    Guid.Parse("10111213-1415-1617-1819-1a1b1c1d1e1f"),
    Guid.Parse("20212223-2425-2627-2829-2a2b2c2d2e2f"),
    1, 0, 2048, 1024, new HashSet<ulong> { 1, 3 });

static LusrSession TestSession() => new(
    Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0, 2048, 1024, new HashSet<ulong> { 1 });

static RadioFrame PostHandshakeFrame(RadioMessageType type, Guid sessionId, Action<CborWriter> extra)
{
    var writer = new CborWriter(CborConformanceMode.Canonical);
    var extraCount = type == RadioMessageType.Heartbeat ? 1 : 3;
    writer.WriteStartMap(2 + extraCount);
    WriteUuid(writer, 0, sessionId);
    WriteUuid(writer, 1, Guid.NewGuid());
    extra(writer);
    writer.WriteEndMap();
    return new(1, 0, (byte)type, FrameCodec.MustUnderstandFlag, writer.Encode());
}

static Guid ReadSessionId(byte[] payload)
{
    var reader = new CborReader(payload, CborConformanceMode.Canonical);
    reader.ReadStartMap();
    AssertEqual((ulong)0, reader.ReadUInt64(), "HELLO_ACK session key missing");
    return new Guid(reader.ReadByteString(), bigEndian: true);
}

static void WriteUuid(CborWriter writer, int key, Guid value)
{
    writer.WriteInt32(key);
    writer.WriteByteString(value.ToByteArray(bigEndian: true));
}

static void WriteUnsigned(CborWriter writer, int key, ulong value)
{
    writer.WriteInt32(key);
    writer.WriteUInt64(value);
}

static async Task AssertProtocolFailure(string expectedCode, Func<Task> action)
{
    try
    {
        await action();
    }
    catch (RadioBridgeProtocolException exception)
    {
        AssertEqual(expectedCode, exception.Code, "Protocol failure code changed");
        return;
    }
    throw new InvalidOperationException("Expected protocol failure did not occur");
}

static async Task<TException> AssertThrows<TException>(Func<Task> action) where TException : Exception
{
    try
    {
        await action();
    }
    catch (TException exception)
    {
        return exception;
    }
    throw new InvalidOperationException($"Expected {typeof(TException).Name} did not occur");
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void AssertEqual<T>(T expected, T actual, string message) where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{message}; expected={expected}, actual={actual}");
}

file sealed class CountingTransport : IRadioByteTransport
{
    public int OpenCount { get; private set; }
    public ValueTask<Stream> OpenAsync(CancellationToken cancellationToken)
    {
        OpenCount++;
        return ValueTask.FromResult<Stream>(new MemoryStream());
    }
}

file sealed class ScriptedDuplexStream(byte[] input, int maximumRead = int.MaxValue) : Stream
{
    private readonly MemoryStream inputStream = new(input, writable: false);
    private readonly MemoryStream outputStream = new();
    public byte[] WrittenBytes => outputStream.ToArray();
    public bool Disposed { get; private set; }
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public override int Read(byte[] buffer, int offset, int count) => inputStream.Read(buffer, offset, Math.Min(count, maximumRead));
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        inputStream.ReadAsync(buffer[..Math.Min(buffer.Length, maximumRead)], cancellationToken);
    public override void Write(byte[] buffer, int offset, int count) => outputStream.Write(buffer, offset, count);
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
        outputStream.WriteAsync(buffer, cancellationToken);
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    protected override void Dispose(bool disposing)
    {
        if (disposing) { Disposed = true; inputStream.Dispose(); outputStream.Dispose(); }
        base.Dispose(disposing);
    }
    public override async ValueTask DisposeAsync()
    {
        Disposed = true;
        await inputStream.DisposeAsync();
        await outputStream.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}

file sealed class LifecycleTransport(params ScriptedDuplexStream[] streams) : IRadioByteTransport
{
    private int next;
    public int OpenCount { get; private set; }

    public async ValueTask<Stream> OpenAsync(CancellationToken cancellationToken)
    {
        OpenCount++;
        if (next < streams.Length) return streams[next++];
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        throw new OperationCanceledException(cancellationToken);
    }
}

file sealed class RecordingSerialConnectionFactory(Exception? openFailure = null) : ISerialConnectionFactory
{
    public int CreateCount { get; private set; }
    public string? DevicePath { get; private set; }
    public int BaudRate { get; private set; }
    public RecordingSerialConnection? Connection { get; private set; }

    public ISerialConnection Create(string devicePath, int baudRate)
    {
        CreateCount++;
        DevicePath = devicePath;
        BaudRate = baudRate;
        Connection = new RecordingSerialConnection(openFailure);
        return Connection;
    }
}

file sealed class RecordingSerialConnection(Exception? openFailure) : ISerialConnection
{
    private readonly MemoryStream stream = new();
    public bool Disposed { get; private set; }

    public Stream Open()
    {
        if (openFailure is not null) throw openFailure;
        return stream;
    }

    public void Dispose()
    {
        Disposed = true;
        stream.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}

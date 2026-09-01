using System.Diagnostics;
using System.Formats.Cbor;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TrailServer.RadioBridge;
using TrailServer.RadioBridge.LinuxIntegrationTests;
using TrailServer.RadioContract;

if (!OperatingSystem.IsLinux())
{
    Console.WriteLine("RADIO_BRIDGE_LINUX_PTY_RESULT=SKIP_NON_LINUX");
    return 0;
}

try
{
    await RunAsync();
    Console.WriteLine("RADIO_BRIDGE_LINUX_PTY_RESULT=PASS");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"RADIO_BRIDGE_LINUX_PTY_RESULT=FAIL type={exception.GetType().Name}");
    return 1;
}

static async Task RunAsync()
{
    const string stableDirectory = "/dev/serial/by-id";
    const string stablePath = stableDirectory + "/SYNTHETIC_TEST_ONLY_trail_radio";
    const string temporaryPath = stableDirectory + "/.SYNTHETIC_TEST_ONLY_next";
    Directory.CreateDirectory(stableDirectory);
    Assert(!File.Exists(stablePath) && !Directory.Exists(stablePath) &&
        !File.Exists(temporaryPath) && !Directory.Exists(temporaryPath),
        "Synthetic stable path already exists");

    await using var first = LinuxPseudoTerminal.Open();
    await using var second = LinuxPseudoTerminal.Open();
    File.CreateSymbolicLink(stablePath, first.SlavePath);

    try
    {
        var options = new RadioBridgeOptions
        {
            Enabled = true,
            Transport = "serial",
            SerialDevicePath = stablePath,
            SerialBaudRate = 115_200,
            HelloTimeoutSeconds = 3,
            HeartbeatTimeoutSeconds = 10,
            ReconnectDelaySeconds = 1,
        };
        var factory = new TrackingSystemSerialConnectionFactory();
        var transport = new ConfiguredRadioByteTransport(options, factory);
        var state = new RadioBridgeState();
        var logger = new CollectingLogger();
        var worker = new ServerRadioBridge(transport, state, Options.Create(options), logger);
        using var overall = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        await worker.StartAsync(overall.Token);
        await WaitUntil(() => factory.CreateCount == 1 && state.GetSnapshot().Phase == RadioBridgePhase.Handshaking,
            overall.Token);
        await WriteFragmentedHelloAsync(first.Master, overall.Token);
        var firstAck = await ReadHelloAckAsync(first.Master, overall.Token);

        File.CreateSymbolicLink(temporaryPath, second.SlavePath);
        File.Move(temporaryPath, stablePath, overwrite: true);
        await first.CloseMasterAsync();

        await WaitUntil(() => factory.CreateCount == 2 && state.GetSnapshot().Phase == RadioBridgePhase.Handshaking,
            overall.Token);
        Assert(factory.OpenIntervals[0] >= TimeSpan.FromMilliseconds(800), "Reconnect delay became a tight loop");
        await WriteFragmentedHelloAsync(second.Master, overall.Token);
        var secondAck = await ReadHelloAckAsync(second.Master, overall.Token);
        Assert(firstAck != secondAck, "Reconnect reused the prior session identity");

        var stopWatch = Stopwatch.StartNew();
        using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await worker.StopAsync(stopTimeout.Token);
        stopWatch.Stop();
        Assert(stopWatch.Elapsed < TimeSpan.FromSeconds(5), "Blocked serial read did not stop within the bound");
        Assert(factory.CreateCount == 2, "Worker opened another serial connection after shutdown");
        Assert(factory.AllDisposed, "Worker did not dispose every serial connection");
        var snapshot = state.GetSnapshot();
        Assert(snapshot.Phase == RadioBridgePhase.Unavailable && snapshot.Reason == "service-stopped",
            "Worker did not finish in the bounded stopped state");
        Assert(logger.Messages.All(IsPrivacySafe), "Worker log exposed transport detail");

        await AssertMissingEndpointIsRedactedAsync();
    }
    finally
    {
        if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        if (File.Exists(stablePath)) File.Delete(stablePath);
    }
}

static async Task WriteFragmentedHelloAsync(Stream master, CancellationToken cancellationToken)
{
    var hello = new LusrHello(
        Guid.Parse("10111213-1415-1617-1819-1a1b1c1d1e1f"),
        Guid.Parse("20212223-2425-2627-2829-2a2b2c2d2e2f"),
        1, 0, 2048, 1024, new HashSet<ulong> { 1, 3 });
    var bytes = FrameCodec.Encode(new RadioFrame(
        1, 0, (byte)RadioMessageType.Hello, FrameCodec.MustUnderstandFlag,
        LusrMessageCodec.EncodeHello(hello)));
    var fragmentSizes = new[] { 1, 2, 3, 5, 8 };
    var offset = 0;
    var fragment = 0;
    while (offset < bytes.Length)
    {
        var count = Math.Min(fragmentSizes[fragment++ % fragmentSizes.Length], bytes.Length - offset);
        await master.WriteAsync(bytes.AsMemory(offset, count), cancellationToken);
        await master.FlushAsync(cancellationToken);
        offset += count;
    }
}

static async Task<Guid> ReadHelloAckAsync(Stream master, CancellationToken cancellationToken)
{
    var frame = await new RadioBridgeFrameReader(master).ReadAsync(cancellationToken);
    Assert(frame.MessageType == (byte)RadioMessageType.HelloAck, "Serial peer received the wrong response");
    Assert(MessagePayloadValidator.Validate(frame).Success, "Serial peer received an invalid HELLO_ACK");
    var reader = new CborReader(frame.Payload, CborConformanceMode.Canonical);
    reader.ReadStartMap();
    Assert(reader.ReadUInt64() == 0, "HELLO_ACK session key changed");
    return new Guid(reader.ReadByteString(), bigEndian: true);
}

static async Task AssertMissingEndpointIsRedactedAsync()
{
    const string missingPath = "/dev/serial/by-id/SYNTHETIC_TEST_ONLY_missing";
    var transport = new ConfiguredRadioByteTransport(Options.Create(new RadioBridgeOptions
    {
        Enabled = true,
        Transport = "serial",
        SerialDevicePath = missingPath,
    }));
    try
    {
        await transport.OpenAsync(CancellationToken.None);
    }
    catch (RadioTransportUnavailableException exception)
    {
        Assert(!exception.ToString().Contains(missingPath, StringComparison.Ordinal),
            "Missing-endpoint error exposed the configured path");
        return;
    }
    throw new InvalidOperationException("Missing serial endpoint unexpectedly opened");
}

static async Task WaitUntil(Func<bool> condition, CancellationToken cancellationToken)
{
    while (!condition()) await Task.Delay(20, cancellationToken);
}

static bool IsPrivacySafe(string message) =>
    !message.Contains("/dev/", StringComparison.OrdinalIgnoreCase) &&
    !message.Contains("pts", StringComparison.OrdinalIgnoreCase) &&
    !message.Contains("SYNTHETIC_TEST_ONLY", StringComparison.OrdinalIgnoreCase) &&
    !message.Contains("exception", StringComparison.OrdinalIgnoreCase);

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

file sealed class TrackingSystemSerialConnectionFactory : ISerialConnectionFactory
{
    private readonly object sync = new();
    private readonly List<TrackingSerialConnection> connections = [];
    private readonly List<DateTimeOffset> openedAt = [];

    public int CreateCount { get { lock (sync) return connections.Count; } }
    public bool AllDisposed { get { lock (sync) return connections.All(connection => connection.Disposed); } }
    public IReadOnlyList<TimeSpan> OpenIntervals
    {
        get
        {
            lock (sync)
                return openedAt.Zip(openedAt.Skip(1), (first, second) => second - first).ToArray();
        }
    }

    public ISerialConnection Create(string devicePath, int baudRate)
    {
        var connection = new TrackingSerialConnection(new SystemSerialConnectionFactory().Create(devicePath, baudRate));
        lock (sync)
        {
            connections.Add(connection);
            openedAt.Add(DateTimeOffset.UtcNow);
        }
        return connection;
    }
}

file sealed class TrackingSerialConnection(ISerialConnection inner) : ISerialConnection
{
    public bool Disposed { get; private set; }
    public Stream Open() => inner.Open();
    public void Dispose()
    {
        Disposed = true;
        inner.Dispose();
    }
    public async ValueTask DisposeAsync()
    {
        Disposed = true;
        await inner.DisposeAsync();
    }
}

file sealed class CollectingLogger : ILogger<ServerRadioBridge>
{
    private readonly List<string> messages = [];
    public IReadOnlyList<string> Messages { get { lock (messages) return messages.ToArray(); } }
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        lock (messages) messages.Add(formatter(state, exception));
    }
}

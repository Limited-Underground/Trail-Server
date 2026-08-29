using System.Formats.Cbor;
using TrailServer.RadioContract;
using TrailServer.RadioContract.SimulatorTests;

var tests = new (string Name, Action Run)[]
{
    ("semantic canonical message fixtures", SemanticMessageFixtures),
    ("corrupt frame resynchronization", CorruptFrameResynchronization),
    ("truncated and oversized frames fail closed", TruncatedAndOversizedFrames),
    ("identity, limits, and capabilities negotiate", IdentityAndLimitNegotiation),
    ("version negotiation rejects major mismatch", VersionNegotiation),
    ("unknown mandatory message terminates session", UnknownMessagePolicy),
    ("credits and duplicate transmit IDs", CreditsAndDuplicateTransmits),
    ("same-boot host restart reconciles IDs", SameBootHostRestart),
    ("USB loss cancels queued but allows active RF completion", UsbLossSemantics),
    ("device reboot makes active work uncertain", DeviceRebootUncertainty),
    ("receive sequence is monotonic and durable before acknowledgement", DurableReceiveBeforeAck),
    ("privacy-safe logger redacts untrusted detail", PrivacySafeLogs),
};

var failures = 0;
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS: {test.Name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.Error.WriteLine($"FAIL: {test.Name}: {exception.Message}");
    }
}

Console.WriteLine();
Console.WriteLine($"RADIO_CONTRACT_SIMULATOR_RESULT={(failures == 0 ? "PASS" : "FAIL")}");
return failures == 0 ? 0 : 1;

static void SemanticMessageFixtures()
{
    var radioId = FixedUuid(0x10);
    var bootId = FixedUuid(0x20);
    var sessionId = FixedUuid(0x30);
    var correlationId = FixedUuid(0x40);

    var fixtures = new[]
    {
        CreateFixture(RadioMessageType.Hello, writer =>
        {
            writer.WriteStartMap(7);
            WriteUuidField(writer, 0, radioId);
            WriteUuidField(writer, 1, bootId);
            WriteUnsignedField(writer, 2, 1);
            WriteUnsignedField(writer, 3, 0);
            WriteUnsignedField(writer, 4, 4140);
            WriteUnsignedField(writer, 5, 4096);
            writer.WriteInt32(6);
            writer.WriteStartArray(2);
            writer.WriteUInt64(1);
            writer.WriteUInt64(2);
            writer.WriteEndArray();
            writer.WriteEndMap();
        }),
        CreateFixture(RadioMessageType.HelloAck, writer =>
        {
            writer.WriteStartMap(6);
            WriteUuidField(writer, 0, sessionId);
            WriteUnsignedField(writer, 1, 1);
            WriteUnsignedField(writer, 2, 0);
            WriteUnsignedField(writer, 3, 4140);
            WriteUnsignedField(writer, 4, 4096);
            writer.WriteInt32(5);
            writer.WriteStartArray(1);
            writer.WriteUInt64(1);
            writer.WriteEndArray();
            writer.WriteEndMap();
        }),
        CreateFixture(RadioMessageType.TxSubmit, writer =>
        {
            writer.WriteStartMap(3);
            WriteUuidField(writer, 0, sessionId);
            WriteUuidField(writer, 1, correlationId);
            writer.WriteInt32(2);
            writer.WriteByteString([0xAA, 0xBB, 0xCC]);
            writer.WriteEndMap();
        }),
        CreateFixture(RadioMessageType.RxPacket, writer =>
        {
            writer.WriteStartMap(7);
            WriteUuidField(writer, 0, sessionId);
            WriteUuidField(writer, 1, correlationId);
            WriteUuidField(writer, 2, bootId);
            WriteUnsignedField(writer, 3, 7);
            writer.WriteInt32(4);
            writer.WriteByteString([0x01, 0x02]);
            writer.WriteInt32(5);
            writer.WriteInt64(-72);
            writer.WriteInt32(6);
            writer.WriteInt64(-8);
            writer.WriteEndMap();
        }),
        CreateFixture(RadioMessageType.Error, writer =>
        {
            writer.WriteStartMap(4);
            WriteUuidField(writer, 0, sessionId);
            WriteUuidField(writer, 1, correlationId);
            writer.WriteInt32(2);
            writer.WriteTextString("checksum");
            writer.WriteInt32(3);
            writer.WriteTextString("frame-discarded");
            writer.WriteEndMap();
        }),
    };

    foreach (var fixture in fixtures)
    {
        var encoded = FrameCodec.Encode(fixture);
        var decoded = FrameCodec.Decode(encoded);
        Assert(decoded.Success, $"{(RadioMessageType)fixture.MessageType} framing failed: {decoded.Error}");
        var validation = MessagePayloadValidator.Validate(decoded.Frame!);
        Assert(validation.Success, $"{(RadioMessageType)fixture.MessageType} schema failed: {validation.Error}");
    }

    var helloHex = Convert.ToHexString(FrameCodec.Encode(fixtures[0]));
    AssertEqual("045452010401013502A72750101112131415161718191A1B1C1D1E1F0150202122232425262728292A2B2C2D2E2F020103080419102C0519100906820102865738BD00", helloHex, "Pinned semantic HELLO fixture changed");

    var malformedHello = CreateFixture(RadioMessageType.Hello, writer =>
    {
        writer.WriteStartMap(1);
        WriteUuidField(writer, 0, radioId);
        writer.WriteEndMap();
    });
    var invalid = MessagePayloadValidator.Validate(malformedHello);
    Assert(!invalid.Success && invalid.Error == "missing-key-1", "Missing required HELLO identity was accepted");
}

static void CorruptFrameResynchronization()
{
    var valid = FrameCodec.Encode(new(1, 0, 0x03, 0, [0xA0]));
    var corrupt = valid.ToArray();
    corrupt[^2] ^= 0x40;
    var results = new FrameStreamDecoder().Feed(corrupt.Concat(valid).ToArray());
    AssertEqual(2, results.Count, "Stream did not produce two bounded records");
    AssertEqual(FrameDecodeError.Checksum, results[0].Error, "Corrupt frame did not fail CRC32C");
    Assert(results[1].Success, "Decoder did not resume at the next delimiter");

    var fragmentedDecoder = new FrameStreamDecoder();
    AssertEqual(0, fragmentedDecoder.Feed(valid.AsSpan(0, 3)).Count, "Partial frame was emitted early");
    var completed = fragmentedDecoder.Feed(valid.AsSpan(3));
    AssertEqual(1, completed.Count, "Fragmented frame did not complete");
    Assert(completed[0].Success, "Fragmented frame failed decode");
}

static void TruncatedAndOversizedFrames()
{
    var valid = FrameCodec.Encode(new(1, 0, 0x03, 0, [0xA0]));
    AssertEqual(FrameDecodeError.MissingDelimiter, FrameCodec.Decode(valid[..^1]).Error, "Missing delimiter was accepted");

    AssertThrows<ArgumentOutOfRangeException>(
        () => FrameCodec.Encode(new(1, 0, 0x20, 0, new byte[FrameCodec.MaximumPayloadLength + 1])),
        "Oversized payload was accepted");

    var oversizedRecord = Enumerable.Repeat((byte)0x01, 4141).Append((byte)0).ToArray();
    var oversizedResult = new FrameStreamDecoder().Feed(oversizedRecord);
    AssertEqual(1, oversizedResult.Count, "Oversized stream record was not bounded");
    AssertEqual(FrameDecodeError.Oversize, oversizedResult[0].Error, "Oversized stream record did not fail closed");
}

static void IdentityAndLimitNegotiation()
{
    var simulator = new SessionSimulator(2);
    var radio = Guid.NewGuid();
    var boot = Guid.NewGuid();
    var session = Guid.NewGuid();
    Assert(simulator.Connect(1, 0, boot, radio, 2048, 1024, new HashSet<ulong> { 1, 3 }, session), "Handshake failed");
    AssertEqual(radio, simulator.RadioId, "Logical radio identity changed");
    AssertEqual(boot, simulator.BootId, "Boot identity changed");
    AssertEqual(session, simulator.SessionId, "Host session identity changed");
    AssertEqual((ushort)2048, simulator.NegotiatedDecodedRecordBytes, "Decoded-record limit was not intersected");
    AssertEqual((ushort)1024, simulator.NegotiatedOpaquePayloadBytes, "Opaque-payload limit was not intersected");
    Assert(simulator.NegotiatedCapabilities.SetEquals([1]), "Capabilities were not intersected");
    AssertEqual(SimulatedTxState.Oversize, simulator.Submit(Guid.NewGuid(), new byte[1025]).State, "Negotiated payload limit was not enforced");
    Assert(simulator.Receive(boot, 1, hasRssi: true), "Negotiated RSSI metadata was rejected");
    Assert(!simulator.Receive(boot, 2, hasSnr: true), "Unnegotiated SNR metadata was accepted");
    Assert(!simulator.Receive(boot, 2, hasChannel: true), "Unnegotiated channel metadata was accepted");

    var validHeartbeat = CreateFixture(RadioMessageType.Heartbeat, writer =>
    {
        writer.WriteStartMap(3);
        WriteUuidField(writer, 0, session.ToByteArray(bigEndian: true));
        WriteUuidField(writer, 1, Guid.NewGuid().ToByteArray(bigEndian: true));
        WriteUnsignedField(writer, 2, 1);
        writer.WriteEndMap();
    });
    Assert(simulator.AcceptMessage(validHeartbeat), "Current session frame was rejected");

    var staleHeartbeat = CreateFixture(RadioMessageType.Heartbeat, writer =>
    {
        writer.WriteStartMap(3);
        WriteUuidField(writer, 0, Guid.NewGuid().ToByteArray(bigEndian: true));
        WriteUuidField(writer, 1, Guid.NewGuid().ToByteArray(bigEndian: true));
        WriteUnsignedField(writer, 2, 2);
        writer.WriteEndMap();
    });
    Assert(!simulator.AcceptMessage(staleHeartbeat), "Stale session frame was accepted");
    Assert(!simulator.Connected, "Stale session frame did not terminate the session");
}

static void VersionNegotiation()
{
    var simulator = new SessionSimulator(2);
    Assert(!simulator.Connect(2, 0, Guid.NewGuid()), "Major mismatch was accepted");
    Assert(simulator.Connect(1, 3, Guid.NewGuid()), "Compatible major was refused");
    AssertEqual((byte)0, simulator.NegotiatedMinor, "Minor negotiation did not use the supported intersection");
}

static void UnknownMessagePolicy()
{
    var simulator = new SessionSimulator(2);
    Assert(simulator.Connect(1, 0, Guid.NewGuid()), "Handshake failed");
    var optional = new RadioFrame(1, 0, 0x55, 0, [0xA0]);
    Assert(simulator.AcceptMessage(optional), "Unknown optional message was not ignorable");
    var required = optional with { Flags = FrameCodec.MustUnderstandFlag };
    Assert(!simulator.AcceptMessage(required), "Unknown required message was accepted");
    Assert(!simulator.Connected, "Unsupported mandatory message did not terminate session");
}

static void CreditsAndDuplicateTransmits()
{
    var simulator = ConnectedSimulator(2, out _);
    var firstId = Guid.NewGuid();
    var secondId = Guid.NewGuid();
    AssertEqual(SimulatedTxState.Accepted, simulator.Submit(firstId).State, "First TX was not accepted");
    AssertEqual(SimulatedTxState.Accepted, simulator.Submit(secondId).State, "Second TX was not accepted");
    AssertEqual(SimulatedTxState.Backpressure, simulator.Submit(Guid.NewGuid()).State, "Capacity overflow did not backpressure");
    Assert(simulator.Submit(firstId).Existing, "Duplicate TX ID was treated as new");
    simulator.StartEmission(firstId);
    simulator.CompleteEmission(firstId);
    simulator.CompleteEmission(firstId);
    AssertEqual(1, simulator.GetTransmission(firstId).EmissionCount, "Duplicate completion created a second emission");
}

static void SameBootHostRestart()
{
    var simulator = ConnectedSimulator(2, out var boot);
    var txId = Guid.NewGuid();
    simulator.Submit(txId);
    simulator.RestartHost();
    Assert(simulator.Connect(1, 0, boot), "Same-boot reconnect failed");
    var repeated = simulator.Submit(txId);
    Assert(repeated.Existing, "Same TX ID was not reconciled after host restart");
    AssertEqual(SimulatedTxState.Accepted, repeated.State, "Same-boot state changed unexpectedly");
}

static void UsbLossSemantics()
{
    var simulator = ConnectedSimulator(2, out var boot);
    var queuedId = Guid.NewGuid();
    var activeId = Guid.NewGuid();
    simulator.Submit(queuedId);
    simulator.Submit(activeId);
    simulator.StartEmission(activeId);
    simulator.Disconnect();
    AssertEqual(SimulatedTxState.Cancelled, simulator.GetTransmission(queuedId).State, "Queued TX was not cancelled on USB loss");
    AssertEqual(SimulatedTxState.Emitting, simulator.GetTransmission(activeId).State, "Active RF was cancelled on USB loss");
    simulator.CompleteEmission(activeId);
    Assert(simulator.Connect(1, 0, boot), "Same-boot reconnect failed");
    AssertEqual(SimulatedTxState.Emitted, simulator.Submit(activeId).State, "Completed active RF was not reconciled");
    AssertEqual(1, simulator.GetTransmission(activeId).EmissionCount, "Active RF did not complete exactly once");
}

static void DeviceRebootUncertainty()
{
    var simulator = ConnectedSimulator(2, out _);
    var txId = Guid.NewGuid();
    simulator.Submit(txId);
    simulator.StartEmission(txId);
    simulator.Disconnect();
    Assert(simulator.Connect(1, 0, Guid.NewGuid()), "Reboot handshake failed");
    AssertEqual(SimulatedTxState.OutcomeUnknown, simulator.GetTransmission(txId).State, "Unresolved active TX was automatically retried");
    AssertEqual(0, simulator.GetTransmission(txId).EmissionCount, "Uncertain TX was represented as emitted");
}

static void DurableReceiveBeforeAck()
{
    var simulator = ConnectedSimulator(2, out var boot);
    Assert(simulator.Receive(boot, 2), "First RX was not admitted");
    Assert(!simulator.Receive(boot, 2), "Duplicate RX identity was admitted twice");
    Assert(!simulator.Receive(boot, 1), "Out-of-order RX sequence was admitted");
    AssertThrows<InvalidOperationException>(() => simulator.AcknowledgeReceive(boot, 2), "RX was acknowledged before durable storage");
    simulator.MarkReceiveDurable(boot, 2);
    simulator.AcknowledgeReceive(boot, 2);
    Assert(simulator.IsReceiveAcknowledged(boot, 2), "Durable RX was not acknowledged");
}

static void PrivacySafeLogs()
{
    var correlationId = Guid.NewGuid();
    var unsafeDetails = new[]
    {
        "PRIVATE_PAYLOAD_BYTES",
        "DEVICE_EFUSE_VALUE",
        "/home/private/radio-key",
        "192.168.50.10",
    };

    foreach (var unsafeDetail in unsafeDetails)
    {
        var log = PrivacySafeError.Format("checksum", correlationId, unsafeDetail);
        Assert(!log.Contains(unsafeDetail, StringComparison.Ordinal), "Logger emitted untrusted detail");
        Assert(log.Contains("detail=redacted", StringComparison.Ordinal), "Logger did not mark redaction");
    }

    var unsafeCode = PrivacySafeError.Format("PRIVATE_PAYLOAD_BYTES", correlationId, "frame-discarded");
    Assert(!unsafeCode.Contains("PRIVATE_PAYLOAD_BYTES", StringComparison.Ordinal), "Logger emitted untrusted error code");
    Assert(unsafeCode.Contains("code=internal", StringComparison.Ordinal), "Logger did not normalize untrusted error code");

    var allowed = PrivacySafeError.Format("checksum", correlationId, "frame-discarded");
    Assert(allowed.Contains("detail=frame-discarded", StringComparison.Ordinal), "Allowlisted detail was removed");
}

static RadioFrame CreateFixture(RadioMessageType type, Action<CborWriter> write)
{
    var writer = new CborWriter(CborConformanceMode.Canonical);
    write(writer);
    return new RadioFrame(1, 0, (byte)type, FrameCodec.MustUnderstandFlag, writer.Encode());
}

static byte[] FixedUuid(byte start) => Enumerable.Range(start, 16).Select(value => (byte)value).ToArray();

static void WriteUuidField(CborWriter writer, int key, byte[] value)
{
    writer.WriteInt32(key);
    writer.WriteByteString(value);
}

static void WriteUnsignedField(CborWriter writer, int key, ulong value)
{
    writer.WriteInt32(key);
    writer.WriteUInt64(value);
}

static SessionSimulator ConnectedSimulator(int capacity, out Guid boot)
{
    var simulator = new SessionSimulator(capacity);
    boot = Guid.NewGuid();
    Assert(simulator.Connect(1, 0, boot), "Handshake failed");
    return simulator;
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertThrows<TException>(Action action, string message)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}

static void AssertEqual<T>(T expected, T actual, string message)
    where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message}; expected={expected}, actual={actual}");
    }
}

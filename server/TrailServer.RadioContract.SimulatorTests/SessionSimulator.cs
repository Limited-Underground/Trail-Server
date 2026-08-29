using TrailServer.RadioContract;

namespace TrailServer.RadioContract.SimulatorTests;

internal enum SimulatedTxState
{
    Accepted,
    Emitting,
    Emitted,
    Failed,
    Cancelled,
    OutcomeUnknown,
    Backpressure,
    Oversize,
}

internal sealed record SimulatedTx(Guid Id, SimulatedTxState State, int EmissionCount);
internal sealed record SubmissionResult(SimulatedTxState State, bool Existing);

internal sealed class SessionSimulator(int transmitCapacity)
{
    private static readonly HashSet<ulong> HostCapabilities = [1, 2];
    private readonly Dictionary<Guid, SimulatedTx> transmissions = [];
    private readonly Dictionary<(Guid BootId, ulong Sequence), SimulatedReceive> receives = [];
    private readonly Dictionary<Guid, ulong> highestReceiveSequence = [];
    private Guid? bootId;

    public bool Connected { get; private set; }
    public Guid RadioId { get; private set; }
    public Guid BootId => bootId ?? Guid.Empty;
    public Guid SessionId { get; private set; }
    public byte NegotiatedMinor { get; private set; }
    public ushort NegotiatedDecodedRecordBytes { get; private set; }
    public ushort NegotiatedOpaquePayloadBytes { get; private set; }
    public IReadOnlySet<ulong> NegotiatedCapabilities { get; private set; } = new HashSet<ulong>();

    public bool Connect(
        byte protocolMajor,
        byte protocolMinor,
        Guid nextBootId,
        Guid? radioId = null,
        ushort maximumDecodedRecordBytes = 4140,
        ushort maximumOpaquePayloadBytes = 4096,
        IReadOnlySet<ulong>? capabilities = null,
        Guid? sessionId = null)
    {
        if (protocolMajor != 1 || maximumDecodedRecordBytes < 12 || maximumOpaquePayloadBytes == 0)
        {
            Connected = false;
            return false;
        }

        if (bootId is not null && bootId != nextBootId)
        {
            foreach (var pair in transmissions.ToArray())
            {
                if (pair.Value.State is SimulatedTxState.Accepted or SimulatedTxState.Emitting)
                {
                    transmissions[pair.Key] = pair.Value with { State = SimulatedTxState.OutcomeUnknown };
                }
            }
        }

        RadioId = radioId ?? (RadioId == Guid.Empty ? Guid.NewGuid() : RadioId);
        bootId = nextBootId;
        SessionId = sessionId ?? Guid.NewGuid();
        NegotiatedMinor = Math.Min(protocolMinor, (byte)0);
        NegotiatedDecodedRecordBytes = Math.Min(maximumDecodedRecordBytes, (ushort)4140);
        NegotiatedOpaquePayloadBytes = Math.Min(maximumOpaquePayloadBytes, (ushort)4096);
        NegotiatedCapabilities = (capabilities ?? new HashSet<ulong>())
            .Where(HostCapabilities.Contains)
            .ToHashSet();
        Connected = true;
        return true;
    }

    public bool AcceptMessage(RadioFrame frame)
    {
        EnsureConnected();
        if (MessagePolicy.MustReject(frame))
        {
            Connected = false;
            return false;
        }

        if (!MessagePolicy.IsKnown(frame.MessageType))
        {
            return true;
        }

        var validation = MessagePayloadValidator.Validate(frame);
        if (!validation.Success)
        {
            Connected = false;
            return false;
        }

        if (frame.MessageType is not ((byte)RadioMessageType.Hello) and not ((byte)RadioMessageType.HelloAck) &&
            (!MessagePayloadValidator.TryGetSessionId(frame, out var frameSessionId) || frameSessionId != SessionId))
        {
            Connected = false;
            return false;
        }

        return true;
    }

    public SubmissionResult Submit(Guid txId, ReadOnlySpan<byte> opaquePayload = default)
    {
        EnsureConnected();
        if (opaquePayload.Length > NegotiatedOpaquePayloadBytes)
        {
            return new(SimulatedTxState.Oversize, Existing: false);
        }

        if (transmissions.TryGetValue(txId, out var existing))
        {
            return new(existing.State, Existing: true);
        }

        var queued = transmissions.Values.Count(value =>
            value.State is SimulatedTxState.Accepted or SimulatedTxState.Emitting);
        if (queued >= transmitCapacity)
        {
            return new(SimulatedTxState.Backpressure, Existing: false);
        }

        transmissions.Add(txId, new(txId, SimulatedTxState.Accepted, 0));
        return new(SimulatedTxState.Accepted, Existing: false);
    }

    public void StartEmission(Guid txId)
    {
        EnsureConnected();
        var current = transmissions[txId];
        if (current.State == SimulatedTxState.Accepted)
        {
            transmissions[txId] = current with { State = SimulatedTxState.Emitting };
        }
    }

    public void CompleteEmission(Guid txId)
    {
        var current = transmissions[txId];
        if (current.State == SimulatedTxState.Emitting)
        {
            transmissions[txId] = current with
            {
                State = SimulatedTxState.Emitted,
                EmissionCount = current.EmissionCount + 1,
            };
        }
    }

    public SimulatedTx GetTransmission(Guid txId) => transmissions[txId];

    public bool Receive(
        Guid receiveBootId,
        ulong sequence,
        bool hasRssi = false,
        bool hasSnr = false,
        bool hasChannel = false)
    {
        EnsureConnected();
        if (receiveBootId != bootId ||
            (hasRssi && !NegotiatedCapabilities.Contains(1)) ||
            (hasSnr && !NegotiatedCapabilities.Contains(2)) ||
            (hasChannel && !NegotiatedCapabilities.Contains(3)) ||
            (highestReceiveSequence.TryGetValue(receiveBootId, out var highest) && sequence <= highest))
        {
            return false;
        }

        var key = (receiveBootId, sequence);
        receives.Add(key, new());
        highestReceiveSequence[receiveBootId] = sequence;
        return true;
    }

    public void MarkReceiveDurable(Guid receiveBootId, ulong sequence) =>
        receives[(receiveBootId, sequence)].Durable = true;

    public void AcknowledgeReceive(Guid receiveBootId, ulong sequence)
    {
        var receive = receives[(receiveBootId, sequence)];
        if (!receive.Durable)
        {
            throw new InvalidOperationException("Receive must be durable before acknowledgement.");
        }

        receive.Acknowledged = true;
    }

    public bool IsReceiveAcknowledged(Guid receiveBootId, ulong sequence) =>
        receives[(receiveBootId, sequence)].Acknowledged;

    public void Disconnect()
    {
        foreach (var pair in transmissions.ToArray())
        {
            if (pair.Value.State == SimulatedTxState.Accepted)
            {
                transmissions[pair.Key] = pair.Value with { State = SimulatedTxState.Cancelled };
            }
        }

        Connected = false;
    }

    public void RestartHost() => Connected = false;

    private void EnsureConnected()
    {
        if (!Connected)
        {
            throw new InvalidOperationException("Handshake is required.");
        }
    }

    private sealed class SimulatedReceive
    {
        public bool Durable { get; set; }
        public bool Acknowledged { get; set; }
    }
}

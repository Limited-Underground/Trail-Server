using System.Formats.Cbor;

namespace TrailServer.RadioContract;

public sealed record PayloadValidationResult(bool Success, string? Error)
{
    public static PayloadValidationResult Pass() => new(true, null);
    public static PayloadValidationResult Fail(string error) => new(false, error);
}

public static class MessagePayloadValidator
{
    private enum FieldKind
    {
        Uuid,
        Unsigned,
        Signed,
        Bytes,
        Text,
        Capabilities,
        ErrorCode,
        SafeDetail,
    }

    public static PayloadValidationResult Validate(RadioFrame frame)
    {
        if (!MessagePolicy.IsKnown(frame.MessageType))
        {
            return PayloadValidationResult.Fail("unknown-message");
        }

        var schema = SchemaFor((RadioMessageType)frame.MessageType);
        try
        {
            var reader = new CborReader(frame.Payload, CborConformanceMode.Canonical);
            var mapLength = reader.ReadStartMap();
            if (mapLength is null)
            {
                return PayloadValidationResult.Fail("indefinite-map");
            }

            var seen = new HashSet<int>();
            var previousKey = -1;
            for (var index = 0; index < mapLength.Value; index++)
            {
                var key = checked((int)reader.ReadUInt64());
                if (key <= previousKey)
                {
                    return PayloadValidationResult.Fail("noncanonical-key-order");
                }

                previousKey = key;
                if (!schema.Fields.TryGetValue(key, out var kind))
                {
                    return PayloadValidationResult.Fail($"unexpected-key-{key}");
                }

                if (!ReadField(reader, kind))
                {
                    return PayloadValidationResult.Fail($"invalid-key-{key}");
                }

                seen.Add(key);
            }

            reader.ReadEndMap();
            if (reader.BytesRemaining != 0)
            {
                return PayloadValidationResult.Fail("trailing-data");
            }

            foreach (var required in schema.Required)
            {
                if (!seen.Contains(required))
                {
                    return PayloadValidationResult.Fail($"missing-key-{required}");
                }
            }

            return PayloadValidationResult.Pass();
        }
        catch (Exception exception) when (
            exception is CborContentException or InvalidOperationException or OverflowException)
        {
            return PayloadValidationResult.Fail("malformed-cbor");
        }
    }

    public static bool TryGetSessionId(RadioFrame frame, out Guid sessionId)
    {
        sessionId = Guid.Empty;
        if (frame.MessageType is (byte)RadioMessageType.Hello or (byte)RadioMessageType.HelloAck ||
            !Validate(frame).Success)
        {
            return false;
        }

        try
        {
            var reader = new CborReader(frame.Payload, CborConformanceMode.Canonical);
            reader.ReadStartMap();
            if (reader.ReadUInt64() != 0)
            {
                return false;
            }

            var bytes = reader.ReadByteString();
            if (bytes.Length != 16)
            {
                return false;
            }

            sessionId = new Guid(bytes, bigEndian: true);
            return true;
        }
        catch (Exception exception) when (
            exception is CborContentException or InvalidOperationException or OverflowException)
        {
            return false;
        }
    }

    private static bool ReadField(CborReader reader, FieldKind kind)
    {
        switch (kind)
        {
            case FieldKind.Uuid:
                return reader.ReadByteString().Length == 16;
            case FieldKind.Unsigned:
                reader.ReadUInt64();
                return true;
            case FieldKind.Signed:
                if (reader.PeekState() == CborReaderState.UnsignedInteger)
                {
                    reader.ReadUInt64();
                    return true;
                }

                if (reader.PeekState() == CborReaderState.NegativeInteger)
                {
                    reader.ReadInt64();
                    return true;
                }

                return false;
            case FieldKind.Bytes:
                return reader.ReadByteString().Length <= FrameCodec.MaximumPayloadLength;
            case FieldKind.Text:
                return reader.ReadTextString().Length <= 64;
            case FieldKind.ErrorCode:
                return PrivacySafeError.IsAllowedCode(reader.ReadTextString());
            case FieldKind.SafeDetail:
                return PrivacySafeError.IsAllowedDetail(reader.ReadTextString());
            case FieldKind.Capabilities:
                var length = reader.ReadStartArray();
                if (length is null || length.Value > 32)
                {
                    return false;
                }

                ulong previous = 0;
                for (var index = 0; index < length.Value; index++)
                {
                    var capability = reader.ReadUInt64();
                    if (capability == 0 || (index > 0 && capability <= previous))
                    {
                        return false;
                    }

                    previous = capability;
                }

                reader.ReadEndArray();
                return true;
            default:
                return false;
        }
    }

    private static (Dictionary<int, FieldKind> Fields, HashSet<int> Required) SchemaFor(
        RadioMessageType type)
    {
        return type switch
        {
            RadioMessageType.Hello => Schema(
                required: [0, 1, 2, 3, 4, 5, 6],
                (0, FieldKind.Uuid), (1, FieldKind.Uuid),
                (2, FieldKind.Unsigned), (3, FieldKind.Unsigned),
                (4, FieldKind.Unsigned), (5, FieldKind.Unsigned),
                (6, FieldKind.Capabilities), (7, FieldKind.Text), (8, FieldKind.Text)),
            RadioMessageType.HelloAck => Schema(
                required: [0, 1, 2, 3, 4, 5],
                (0, FieldKind.Uuid), (1, FieldKind.Unsigned),
                (2, FieldKind.Unsigned), (3, FieldKind.Unsigned),
                (4, FieldKind.Unsigned), (5, FieldKind.Capabilities)),
            RadioMessageType.Heartbeat => PostHandshake([2], (2, FieldKind.Unsigned)),
            RadioMessageType.Status => PostHandshake(
                [2, 3, 4, 5, 6],
                (2, FieldKind.Unsigned), (3, FieldKind.Unsigned),
                (4, FieldKind.Unsigned), (5, FieldKind.Unsigned),
                (6, FieldKind.Unsigned)),
            RadioMessageType.TxSubmit => PostHandshake(
                [2],
                (2, FieldKind.Bytes), (3, FieldKind.Unsigned)),
            RadioMessageType.TxAccepted => PostHandshake([]),
            RadioMessageType.TxResult => PostHandshake(
                [2],
                (2, FieldKind.Unsigned), (3, FieldKind.ErrorCode)),
            RadioMessageType.RxPacket => PostHandshake(
                [2, 3, 4],
                (2, FieldKind.Uuid), (3, FieldKind.Unsigned),
                (4, FieldKind.Bytes), (5, FieldKind.Signed),
                (6, FieldKind.Signed), (7, FieldKind.Unsigned)),
            RadioMessageType.RxAck => PostHandshake(
                [2, 3],
                (2, FieldKind.Uuid), (3, FieldKind.Unsigned)),
            RadioMessageType.Error => PostHandshake(
                [2],
                (2, FieldKind.ErrorCode), (3, FieldKind.SafeDetail)),
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
    }

    private static (Dictionary<int, FieldKind> Fields, HashSet<int> Required) PostHandshake(
        int[] required,
        params (int Key, FieldKind Kind)[] fields)
    {
        var allFields = new List<(int Key, FieldKind Kind)>
        {
            (0, FieldKind.Uuid),
            (1, FieldKind.Uuid),
        };
        allFields.AddRange(fields);
        return Schema([0, 1, .. required], [.. allFields]);
    }

    private static (Dictionary<int, FieldKind> Fields, HashSet<int> Required) Schema(
        int[] required,
        params (int Key, FieldKind Kind)[] fields) =>
        (fields.ToDictionary(field => field.Key, field => field.Kind), [.. required]);

}

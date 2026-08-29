using System.Formats.Cbor;
using TrailServer.RadioContract;

namespace TrailServer.RadioBridge;

public sealed record LusrHello(
    Guid RadioId,
    Guid BootId,
    byte ProtocolMajor,
    byte MaximumMinor,
    ushort MaximumDecodedRecordBytes,
    ushort MaximumOpaquePayloadBytes,
    IReadOnlySet<ulong> Capabilities);

public sealed record LusrSession(
    Guid SessionId,
    Guid RadioId,
    Guid BootId,
    byte ProtocolMinor,
    ushort MaximumDecodedRecordBytes,
    ushort MaximumOpaquePayloadBytes,
    IReadOnlySet<ulong> Capabilities);

public static class LusrMessageCodec
{
    public static bool TryDecodeHello(RadioFrame frame, out LusrHello? hello)
    {
        hello = null;
        if (frame.MessageType != (byte)RadioMessageType.Hello ||
            !MessagePayloadValidator.Validate(frame).Success)
        {
            return false;
        }

        try
        {
            var reader = new CborReader(frame.Payload, CborConformanceMode.Canonical);
            var count = reader.ReadStartMap()!.Value;
            Guid radioId = Guid.Empty;
            Guid bootId = Guid.Empty;
            byte major = 0;
            byte minor = 0;
            ushort decoded = 0;
            ushort opaque = 0;
            var capabilities = new HashSet<ulong>();
            for (var index = 0; index < count; index++)
            {
                switch (reader.ReadUInt64())
                {
                    case 0: radioId = new Guid(reader.ReadByteString(), bigEndian: true); break;
                    case 1: bootId = new Guid(reader.ReadByteString(), bigEndian: true); break;
                    case 2: major = checked((byte)reader.ReadUInt64()); break;
                    case 3: minor = checked((byte)reader.ReadUInt64()); break;
                    case 4: decoded = checked((ushort)reader.ReadUInt64()); break;
                    case 5: opaque = checked((ushort)reader.ReadUInt64()); break;
                    case 6:
                        var length = reader.ReadStartArray()!.Value;
                        for (var capability = 0; capability < length; capability++)
                        {
                            capabilities.Add(reader.ReadUInt64());
                        }
                        reader.ReadEndArray();
                        break;
                    case 7 or 8: reader.ReadTextString(); break;
                    default: return false;
                }
            }
            reader.ReadEndMap();
            hello = new(radioId, bootId, major, minor, decoded, opaque, capabilities);
            return true;
        }
        catch (Exception exception) when (exception is CborContentException or InvalidOperationException or OverflowException)
        {
            return false;
        }
    }

    public static byte[] EncodeHello(LusrHello hello)
    {
        var writer = new CborWriter(CborConformanceMode.Canonical);
        writer.WriteStartMap(7);
        WriteUuid(writer, 0, hello.RadioId);
        WriteUuid(writer, 1, hello.BootId);
        WriteUnsigned(writer, 2, hello.ProtocolMajor);
        WriteUnsigned(writer, 3, hello.MaximumMinor);
        WriteUnsigned(writer, 4, hello.MaximumDecodedRecordBytes);
        WriteUnsigned(writer, 5, hello.MaximumOpaquePayloadBytes);
        writer.WriteInt32(6);
        WriteCapabilities(writer, hello.Capabilities);
        writer.WriteEndMap();
        return writer.Encode();
    }

    public static byte[] EncodeHelloAck(LusrSession session)
    {
        var writer = new CborWriter(CborConformanceMode.Canonical);
        writer.WriteStartMap(6);
        WriteUuid(writer, 0, session.SessionId);
        WriteUnsigned(writer, 1, 1);
        WriteUnsigned(writer, 2, session.ProtocolMinor);
        WriteUnsigned(writer, 3, session.MaximumDecodedRecordBytes);
        WriteUnsigned(writer, 4, session.MaximumOpaquePayloadBytes);
        writer.WriteInt32(5);
        WriteCapabilities(writer, session.Capabilities);
        writer.WriteEndMap();
        return writer.Encode();
    }

    private static void WriteUuid(CborWriter writer, int key, Guid value)
    {
        writer.WriteInt32(key);
        writer.WriteByteString(value.ToByteArray(bigEndian: true));
    }

    private static void WriteUnsigned(CborWriter writer, int key, ulong value)
    {
        writer.WriteInt32(key);
        writer.WriteUInt64(value);
    }

    private static void WriteCapabilities(CborWriter writer, IReadOnlySet<ulong> capabilities)
    {
        var ordered = capabilities.Order().ToArray();
        writer.WriteStartArray(ordered.Length);
        foreach (var capability in ordered) writer.WriteUInt64(capability);
        writer.WriteEndArray();
    }
}

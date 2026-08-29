using System.Buffers.Binary;

namespace TrailServer.RadioContract;

public static class FrameCodec
{
    public const int MaximumPayloadLength = 4096;
    public const byte MustUnderstandFlag = 0x01;
    private const int HeaderLength = 8;
    private const int ChecksumLength = 4;

    public static byte[] Encode(RadioFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(frame.Payload);
        if (frame.Payload.Length > MaximumPayloadLength)
        {
            throw new ArgumentOutOfRangeException(nameof(frame), "Payload exceeds the LUSR/1 limit.");
        }

        if ((frame.Flags & ~MustUnderstandFlag) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frame), "Reserved LUSR/1 flags must be zero.");
        }

        var decoded = new byte[HeaderLength + frame.Payload.Length + ChecksumLength];
        decoded[0] = 0x54;
        decoded[1] = 0x52;
        decoded[2] = frame.ProtocolMajor;
        decoded[3] = frame.ProtocolMinor;
        decoded[4] = frame.MessageType;
        decoded[5] = frame.Flags;
        BinaryPrimitives.WriteUInt16LittleEndian(decoded.AsSpan(6, 2), checked((ushort)frame.Payload.Length));
        frame.Payload.CopyTo(decoded, HeaderLength);
        var checksum = Crc32C.Compute(decoded.AsSpan(0, decoded.Length - ChecksumLength));
        BinaryPrimitives.WriteUInt32LittleEndian(decoded.AsSpan(decoded.Length - ChecksumLength), checksum);
        return CobsCodec.Encode(decoded);
    }

    public static FrameDecodeResult Decode(ReadOnlySpan<byte> record)
    {
        if (record.IsEmpty || record[^1] != 0)
        {
            return new(null, FrameDecodeError.MissingDelimiter);
        }

        if (!CobsCodec.TryDecode(record[..^1], out var decoded))
        {
            return new(null, FrameDecodeError.MalformedCobs);
        }

        if (decoded.Length < HeaderLength + ChecksumLength)
        {
            return new(null, FrameDecodeError.Truncated);
        }

        if (decoded[0] != 0x54 || decoded[1] != 0x52)
        {
            return new(null, FrameDecodeError.BadMagic);
        }

        if ((decoded[5] & ~MustUnderstandFlag) != 0)
        {
            return new(null, FrameDecodeError.ReservedFlags);
        }

        var payloadLength = BinaryPrimitives.ReadUInt16LittleEndian(decoded.AsSpan(6, 2));
        if (payloadLength > MaximumPayloadLength)
        {
            return new(null, FrameDecodeError.Oversize);
        }

        var expectedLength = HeaderLength + payloadLength + ChecksumLength;
        if (decoded.Length != expectedLength)
        {
            return new(null, FrameDecodeError.LengthMismatch);
        }

        var expectedChecksum = BinaryPrimitives.ReadUInt32LittleEndian(decoded.AsSpan(decoded.Length - ChecksumLength));
        var actualChecksum = Crc32C.Compute(decoded.AsSpan(0, decoded.Length - ChecksumLength));
        if (expectedChecksum != actualChecksum)
        {
            return new(null, FrameDecodeError.Checksum);
        }

        return new(
            new RadioFrame(
                decoded[2],
                decoded[3],
                decoded[4],
                decoded[5],
                decoded.AsSpan(HeaderLength, payloadLength).ToArray()),
            FrameDecodeError.None);
    }
}

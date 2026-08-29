namespace TrailServer.RadioContract;

internal static class CobsCodec
{
    public static byte[] Encode(ReadOnlySpan<byte> input)
    {
        var output = new byte[input.Length + (input.Length / 254) + 2];
        var codeIndex = 0;
        var writeIndex = 1;
        byte code = 1;

        foreach (var value in input)
        {
            if (value == 0)
            {
                output[codeIndex] = code;
                codeIndex = writeIndex++;
                code = 1;
                continue;
            }

            output[writeIndex++] = value;
            code++;
            if (code == 0xFF)
            {
                output[codeIndex] = code;
                codeIndex = writeIndex++;
                code = 1;
            }
        }

        output[codeIndex] = code;
        output[writeIndex++] = 0;
        Array.Resize(ref output, writeIndex);
        return output;
    }

    public static bool TryDecode(ReadOnlySpan<byte> encoded, out byte[] decoded)
    {
        decoded = new byte[encoded.Length];
        var readIndex = 0;
        var writeIndex = 0;

        while (readIndex < encoded.Length)
        {
            var code = encoded[readIndex++];
            if (code == 0)
            {
                decoded = [];
                return false;
            }

            var copyCount = code - 1;
            if (readIndex + copyCount > encoded.Length)
            {
                decoded = [];
                return false;
            }

            encoded.Slice(readIndex, copyCount).CopyTo(decoded.AsSpan(writeIndex));
            readIndex += copyCount;
            writeIndex += copyCount;

            if (code != 0xFF && readIndex < encoded.Length)
            {
                decoded[writeIndex++] = 0;
            }
        }

        Array.Resize(ref decoded, writeIndex);
        return true;
    }
}

using System.Runtime.InteropServices;

namespace TrailServer.RadioContract;

public sealed class FrameStreamDecoder
{
    private const int MaximumEncodedRecordLength = 4140;
    private readonly List<byte> buffer = [];
    private bool overflowed;

    public IReadOnlyList<FrameDecodeResult> Feed(ReadOnlySpan<byte> bytes)
    {
        var results = new List<FrameDecodeResult>();
        foreach (var value in bytes)
        {
            if (value != 0)
            {
                if (buffer.Count < MaximumEncodedRecordLength)
                {
                    buffer.Add(value);
                }
                else
                {
                    overflowed = true;
                }

                continue;
            }

            if (overflowed)
            {
                results.Add(new(null, FrameDecodeError.Oversize));
                buffer.Clear();
                overflowed = false;
                continue;
            }

            if (buffer.Count == 0)
            {
                results.Add(new(null, FrameDecodeError.Truncated));
                continue;
            }

            buffer.Add(0);
            results.Add(FrameCodec.Decode(CollectionsMarshal.AsSpan(buffer)));
            buffer.Clear();
        }

        return results;
    }
}

namespace TrailServer.RadioContract;

internal static class Crc32C
{
    private const uint ReversedPolynomial = 0x82F63B78u;

    public static uint Compute(ReadOnlySpan<byte> data)
    {
        var crc = uint.MaxValue;
        foreach (var value in data)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc >> 1) ^ ((crc & 1) == 0 ? 0 : ReversedPolynomial);
            }
        }

        return ~crc;
    }
}

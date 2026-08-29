namespace TrailServer.RadioContract;

public sealed record RadioFrame(
    byte ProtocolMajor,
    byte ProtocolMinor,
    byte MessageType,
    byte Flags,
    byte[] Payload);

public enum FrameDecodeError
{
    None,
    MissingDelimiter,
    MalformedCobs,
    Truncated,
    BadMagic,
    ReservedFlags,
    Oversize,
    LengthMismatch,
    Checksum,
}

public sealed record FrameDecodeResult(RadioFrame? Frame, FrameDecodeError Error)
{
    public bool Success => Frame is not null && Error == FrameDecodeError.None;
}

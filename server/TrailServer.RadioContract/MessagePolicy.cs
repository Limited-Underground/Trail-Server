namespace TrailServer.RadioContract;

public enum RadioMessageType : byte
{
    Hello = 0x01,
    HelloAck = 0x02,
    Heartbeat = 0x03,
    Status = 0x04,
    TxSubmit = 0x10,
    TxAccepted = 0x11,
    TxResult = 0x12,
    RxPacket = 0x20,
    RxAck = 0x21,
    Error = 0x7F,
}

public static class MessagePolicy
{
    public static bool IsKnown(byte messageType) =>
        Enum.IsDefined(typeof(RadioMessageType), messageType);

    public static bool MustReject(RadioFrame frame) =>
        !IsKnown(frame.MessageType) &&
        (frame.Flags & FrameCodec.MustUnderstandFlag) != 0;
}

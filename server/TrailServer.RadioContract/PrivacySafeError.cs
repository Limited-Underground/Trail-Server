namespace TrailServer.RadioContract;

public static class PrivacySafeError
{
    private static readonly HashSet<string> AllowedCodes =
    [
        "protocol_version", "malformed_frame", "checksum", "oversize", "unsupported",
        "invalid_state", "queue_full", "backpressure", "busy", "expired",
        "radio_unavailable", "tx_failed", "internal",
    ];

    private static readonly HashSet<string> AllowedDetails =
    [
        "frame-discarded",
        "retry-later",
        "session-reset-required",
    ];

    public static string NormalizeDetail(string? untrustedDetail) =>
        IsAllowedDetail(untrustedDetail)
            ? untrustedDetail!
            : "redacted";

    public static bool IsAllowedDetail(string? detail) =>
        detail is not null && AllowedDetails.Contains(detail);

    public static bool IsAllowedCode(string? code) =>
        code is not null && AllowedCodes.Contains(code);

    public static string NormalizeCode(string? untrustedCode) =>
        IsAllowedCode(untrustedCode) ? untrustedCode! : "internal";

    public static string Format(string code, Guid correlationId, string? untrustedDetail) =>
        $"radio_error code={NormalizeCode(code)} correlation_id={correlationId:D} detail={NormalizeDetail(untrustedDetail)}";
}

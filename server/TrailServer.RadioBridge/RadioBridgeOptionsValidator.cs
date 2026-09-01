using Microsoft.Extensions.Options;

namespace TrailServer.RadioBridge;

public sealed class RadioBridgeOptionsValidator : IValidateOptions<RadioBridgeOptions>
{
    public ValidateOptionsResult Validate(string? name, RadioBridgeOptions options)
    {
        var transport = options.Transport ?? string.Empty;
        if (!string.Equals(transport, transport.Trim(), StringComparison.Ordinal))
            return ValidateOptionsResult.Fail("RadioBridge Transport must not contain surrounding whitespace.");
        transport = transport.ToLowerInvariant();
        if (transport is not ("disabled" or "serial"))
            return ValidateOptionsResult.Fail("RadioBridge Transport must be disabled or serial.");

        if (options.Enabled && transport != "serial")
            return ValidateOptionsResult.Fail("An enabled RadioBridge requires the serial transport.");

        if (transport == "serial" && !RadioSerialDevicePath.IsStable(options.SerialDevicePath))
            return ValidateOptionsResult.Fail("SerialDevicePath must be one stable /dev/serial/by-id device path.");

        if (transport == "disabled" && !string.IsNullOrWhiteSpace(options.SerialDevicePath))
            return ValidateOptionsResult.Fail("SerialDevicePath must be omitted when Transport is disabled.");

        return ValidateOptionsResult.Success;
    }
}

public static class RadioSerialDevicePath
{
    public const string StablePrefix = "/dev/serial/by-id/";

    public static bool IsStable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 512 ||
            !value.StartsWith(StablePrefix, StringComparison.Ordinal))
            return false;

        var name = value[StablePrefix.Length..];
        return name.Length > 0 && name is not "." and not ".." &&
            !name.Contains('/') && !name.Contains('\\') &&
            name.All(character => !char.IsControl(character));
    }
}

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Text;

namespace TrailServer.RadioBridge.LinuxIntegrationTests;

internal sealed class LinuxPseudoTerminal : IAsyncDisposable
{
    private FileStream? master;

    private LinuxPseudoTerminal(FileStream master, string slavePath)
    {
        this.master = master;
        SlavePath = slavePath;
    }

    public FileStream Master => master ?? throw new ObjectDisposedException(nameof(LinuxPseudoTerminal));
    public string SlavePath { get; }

    public static LinuxPseudoTerminal Open()
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException();

        const int readWrite = 0x0002;
        const int noControllingTerminal = 0x0100;
        const int closeOnExec = 0x80000;
        var descriptor = Native.posix_openpt(readWrite | noControllingTerminal | closeOnExec);
        if (descriptor < 0) throw Native.Failure("posix_openpt");

        var handle = new SafeFileHandle((nint)descriptor, ownsHandle: true);
        try
        {
            if (Native.grantpt(descriptor) != 0) throw Native.Failure("grantpt");
            if (Native.unlockpt(descriptor) != 0) throw Native.Failure("unlockpt");

            var pathBytes = new byte[256];
            if (Native.ptsname_r(descriptor, pathBytes, (nuint)pathBytes.Length) != 0)
                throw Native.Failure("ptsname_r");
            var terminator = Array.IndexOf(pathBytes, (byte)0);
            var slavePath = Encoding.UTF8.GetString(pathBytes, 0, terminator < 0 ? pathBytes.Length : terminator);
            if (!slavePath.StartsWith("/dev/pts/", StringComparison.Ordinal))
                throw new InvalidOperationException("Pseudo-terminal returned an unexpected slave namespace");

            var stream = new FileStream(handle, FileAccess.ReadWrite, 4096, isAsync: false);
            handle = null!;
            return new LinuxPseudoTerminal(stream, slavePath);
        }
        finally
        {
            handle?.Dispose();
        }
    }

    public async ValueTask CloseMasterAsync()
    {
        if (master is null) return;
        await master.DisposeAsync();
        master = null;
    }

    public ValueTask DisposeAsync() => CloseMasterAsync();

    private static class Native
    {
        [DllImport("libc", SetLastError = true)]
        internal static extern int posix_openpt(int flags);

        [DllImport("libc", SetLastError = true)]
        internal static extern int grantpt(int descriptor);

        [DllImport("libc", SetLastError = true)]
        internal static extern int unlockpt(int descriptor);

        [DllImport("libc", SetLastError = true)]
        internal static extern int ptsname_r(int descriptor, [Out] byte[] buffer, nuint length);

        internal static IOException Failure(string operation) =>
            new($"{operation} failed with errno {Marshal.GetLastPInvokeError()}");
    }
}

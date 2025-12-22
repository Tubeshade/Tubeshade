using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

// ReSharper disable InconsistentNaming

namespace Ytdlp.Processes;

[SupportedOSPlatform("linux")]
public static partial class libc
{
    /// <summary>Send signal to a process.</summary>
    /// <returns>
    /// On success, zero is returned.
    /// If signals were sent to a process group, success means that at least one signal was delivered.
    /// On error, -1 is returned, and errno is set to indicate the error.
    /// </returns>
    /// <seealso href="https://man7.org/linux/man-pages/man2/kill.2.html"/>
    [LibraryImport(nameof(libc), SetLastError = true)]
    internal static partial int kill(int pid, int sig);

    [LibraryImport(nameof(libc), SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
    public static partial int mkfifo(string path, Mode mode_t);

    internal enum Signals
    {
        /// <summary>Termination signal.</summary>
        SIGTERM = 15,
    }

    [Flags]
    public enum Mode
    {
        S_IRUSR = 0x100,
        S_IWUSR = 0x80,
        S_IXUSR = 0x40,

        S_IRGRP = 0x20,
        S_IWGRP = 0x10,
        S_IXGRP = 0x8,

        S_IROTH = 0x4,
        S_IWOTH = 0x2,
        S_IXOTH = 0x1,

        S_IRWXU = S_IRUSR | S_IWUSR | S_IXUSR,
        S_IRWXG = S_IRGRP | S_IWGRP | S_IXGRP,
        S_IRWXO = S_IROTH | S_IWOTH | S_IXOTH,
    }
}

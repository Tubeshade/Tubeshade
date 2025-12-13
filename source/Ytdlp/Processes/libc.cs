using System.Runtime.InteropServices;
using System.Runtime.Versioning;

// ReSharper disable InconsistentNaming

namespace Ytdlp.Processes;

[SupportedOSPlatform("linux")]
internal static partial class libc
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

    internal enum Signals
    {
        /// <summary>Termination signal.</summary>
        SIGTERM = 15,
    }
}

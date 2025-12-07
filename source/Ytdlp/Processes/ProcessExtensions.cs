using System;
using System.Diagnostics;
using static Ytdlp.Processes.libc;

namespace Ytdlp.Processes;

internal static class ProcessExtensions
{
    internal static bool TryTerminate(this Process process)
    {
        if (OperatingSystem.IsLinux())
        {
            var returnCode = kill(process.Id, (int)Signals.SIGTERM);
            return returnCode is 0;
        }

        if (OperatingSystem.IsWindows())
        {
            var taskKillProcess = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "taskkill",
                    Arguments = $"/T /F /PID {process.Id}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

            taskKillProcess?.WaitForExit();
        }

        return false;
    }
}

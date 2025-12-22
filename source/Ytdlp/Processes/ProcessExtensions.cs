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
            var returnCode = -1;

            for (var index = 0; index < 3; index++)
            {
                returnCode = kill(process.Id, (int)Signals.SIGTERM);
                if (returnCode is not 0)
                {
                    return false;
                }
            }

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

            if (taskKillProcess is not null)
            {
                taskKillProcess.WaitForExit();
                return taskKillProcess.ExitCode is 0;
            }
        }

        return false;
    }
}

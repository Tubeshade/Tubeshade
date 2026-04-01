using System;
using System.Threading.Tasks;
using Ytdlp.Processes;

namespace Tubeshade.Server.Services;

internal static class TaskExtensions
{
    internal static void ThrowIfNonSuccessfulExitCode(this Task<int> processTask, CancelableProcess process)
    {
        if (processTask is not { IsCompleted: true, Result: not 0 })
        {
            return;
        }

        var videoError = string.Join(Environment.NewLine, process.ErrorLines);
        throw new(videoError);
    }
}

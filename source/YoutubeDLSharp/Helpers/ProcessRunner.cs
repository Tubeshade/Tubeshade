using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YoutubeDLSharp.Options;

namespace YoutubeDLSharp.Helpers;

/// <summary>
/// Provides methods for throttled execution of processes.
/// </summary>
public sealed class ProcessRunner
{
    public async Task<(int, string?[])> RunThrottled(
        YoutubeDLProcess process,
        string[] urls,
        OptionSet options,
        CancellationToken ct,
        IProgress<DownloadProgress>? progress = null)
    {
        var errors = new List<string?>();
        process.ErrorReceived += (_, args) => errors.Add(args.Data);

        var exitCode = await process.RunAsync(urls, options, ct, progress);
        return (exitCode, errors.ToArray());
    }
}

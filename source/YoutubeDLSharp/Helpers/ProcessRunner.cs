using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YoutubeDLSharp.Options;

namespace YoutubeDLSharp.Helpers;

internal static class ProcessRunner
{
    internal static async Task<(int, string?[])> RunThrottled(
        YoutubeDLProcess process,
        string[] urls,
        OptionSet options,
        CancellationToken cancellationToken)
    {
        var errors = new List<string?>();
        process.ErrorReceived += (_, args) => errors.Add(args.Data);

        var exitCode = await process.RunAsync(urls, options, cancellationToken);
        return (exitCode, errors.ToArray());
    }
}

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YoutubeDLSharp.Helpers;
using YoutubeDLSharp.Options;

namespace YoutubeDLSharp;

/// <summary>
/// A low-level wrapper for the yt-dlp executable.
/// </summary>
public sealed class YoutubeDLProcess
{
    private readonly string _executablePath;

    /// <summary>
    /// Occurs each time yt-dlp writes to the standard output.
    /// </summary>
    public event EventHandler<DataReceivedEventArgs>? OutputReceived;

    /// <summary>
    /// Occurs each time yt-dlp writes to the error output.
    /// </summary>
    public event EventHandler<DataReceivedEventArgs>? ErrorReceived;

    /// <summary>
    /// Creates a new instance of the YoutubeDLProcess class.
    /// </summary>
    /// <param name="executablePath">The path to the yt-dlp executable.</param>
    public YoutubeDLProcess(string executablePath)
    {
        _executablePath = executablePath;
    }

    /// <summary>
    /// Invokes yt-dlp with the specified parameters and options.
    /// </summary>
    /// <param name="urls">The video URLs to be passed to yt-dlp.</param>
    /// <param name="options">An OptionSet specifying the options to be passed to yt-dlp.</param>
    /// <param name="cancellationToken">A CancellationToken used to cancel the download.</param>
    /// <returns>The exit code of the yt-dlp process.</returns>
    public async Task<int> RunAsync(
        string[]? urls,
        OptionSet options,
        CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<int>();
        var process = new Process();
        var startInfo = new ProcessStartInfo
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            FileName = _executablePath,
            Arguments = ConvertToArgs(urls, options)
        };

        process.EnableRaisingEvents = true;
        process.StartInfo = startInfo;
        var tcsOut = new TaskCompletionSource<bool>();
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null)
            {
                tcsOut.SetResult(true);
                return;
            }

            OutputReceived?.Invoke(this, e);
        };
        var tcsError = new TaskCompletionSource<bool>();
        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data == null)
            {
                tcsError.SetResult(true);
                return;
            }

            ErrorReceived?.Invoke(this, args);
        };
        process.Exited += async (_, _) =>
        {
            // Wait for output and error streams to finish
            await tcsOut.Task;
            await tcsError.Task;
            tcs.TrySetResult(process.ExitCode);
            process.Dispose();
        };
        cancellationToken.Register(() =>
        {
            if (!tcs.Task.IsCompleted)
            {
                tcs.TrySetCanceled();
            }

            try
            {
                if (!process.HasExited)
                {
                    process.KillTree();
                }
            }
            catch
            {
                // ignored
            }
        });

        if (!await Task.Run(() => process.Start()))
        {
            tcs.TrySetException(new InvalidOperationException("Failed to start yt-dlp process."));
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return await tcs.Task;
    }

    private static string ConvertToArgs(string[]? urls, OptionSet options)
    {
        return $"{options} -- {(urls != null ? string.Join(" ", urls.Select(s => $"\"{s}\"")) : string.Empty)}";
    }
}

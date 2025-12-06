using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    // the regex used to match the currently downloaded video of a playlist.
    private static readonly Regex RgxPlaylist = new(@"Downloading video (\d+) of (\d+)", RegexOptions.Compiled);

    // the regex used for matching download progress information.
    private static readonly Regex RgxProgress = new(
        @"\[download\]\s+(?:(?<percent>[\d\.]+)%(?:\s+of\s+\~?\s*(?<total>[\d\.\w]+))?\s+at\s+(?:(?<speed>[\d\.\w]+\/s)|[\w\s]+)\s+ETA\s(?<eta>[\d\:]+))?",
        RegexOptions.Compiled
    );

    // the regex used to match the beginning of post-processing.
    private static readonly Regex RgxPost = new(@"\[(\w+)\]\s+", RegexOptions.Compiled);

    /// <summary>
    /// The path to the Python interpreter.
    /// If this property is non-empty, yt-dlp will be run using the Python interpreter.
    /// In this case, ExecutablePath should point to a non-binary, Python version of yt-dlp.
    /// </summary>
    public string? PythonPath { get; set; }

    /// <summary>
    /// The path to the yt-dlp executable.
    /// </summary>
    public string ExecutablePath { get; set; }

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
    public YoutubeDLProcess(string executablePath = "yt-dlp.exe")
    {
        ExecutablePath = executablePath;
    }

    internal static string ConvertToArgs(string[]? urls, OptionSet options)
    {
        return $"{options} -- {(urls != null ? string.Join(" ", urls.Select(s => $"\"{s}\"")) : string.Empty)}";
    }

    internal void RedirectToError(DataReceivedEventArgs e)
        => ErrorReceived?.Invoke(this, e);

    /// <summary>
    /// Invokes yt-dlp with the specified parameters and options.
    /// </summary>
    /// <param name="urls">The video URLs to be passed to yt-dlp.</param>
    /// <param name="options">An OptionSet specifying the options to be passed to yt-dlp.</param>
    /// <returns>The exit code of the yt-dlp process.</returns>
    public async Task<int> RunAsync(string[]? urls, OptionSet options)
        => await RunAsync(urls, options, CancellationToken.None);

    /// <summary>
    /// Invokes yt-dlp with the specified parameters and options.
    /// </summary>
    /// <param name="urls">The video URLs to be passed to yt-dlp.</param>
    /// <param name="options">An OptionSet specifying the options to be passed to yt-dlp.</param>
    /// <param name="ct">A CancellationToken used to cancel the download.</param>
    /// <param name="progress">A progress provider used to get download progress information.</param>
    /// <returns>The exit code of the yt-dlp process.</returns>
    public async Task<int> RunAsync(
        string[]? urls,
        OptionSet options,
        CancellationToken ct,
        IProgress<DownloadProgress>? progress = null)
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
        };
        if (!string.IsNullOrEmpty(PythonPath))
        {
            startInfo.FileName = PythonPath;
            startInfo.Arguments = $"\"{ExecutablePath}\" {ConvertToArgs(urls, options)}";
        }
        else
        {
            startInfo.FileName = ExecutablePath;
            startInfo.Arguments = ConvertToArgs(urls, options);
        }

        process.EnableRaisingEvents = true;
        process.StartInfo = startInfo;
        var tcsOut = new TaskCompletionSource<bool>();
        // this variable is used for tracking download states
        var isDownloading = false;
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null)
            {
                tcsOut.SetResult(true);
                return;
            }

            Match match;
            if ((match = RgxProgress.Match(e.Data)).Success)
            {
                if (match.Groups.Count > 1 && match.Groups[1].Length > 0)
                {
                    var progValue = float.Parse(match.Groups[1].ToString(), CultureInfo.InvariantCulture) / 100.0f;
                    var totalGroup = match.Groups["total"];
                    var total = totalGroup.Success ? totalGroup.Value : null;
                    var speedGroup = match.Groups["speed"];
                    var speed = speedGroup.Success ? speedGroup.Value : null;
                    var etaGroup = match.Groups["eta"];
                    var eta = etaGroup.Success ? etaGroup.Value : null;
                    progress?.Report(
                        new DownloadProgress(
                            DownloadState.Downloading, progress: progValue, totalDownloadSize: total,
                            downloadSpeed: speed, eta: eta
                        )
                    );
                }
                else
                {
                    progress?.Report(new DownloadProgress(DownloadState.Downloading));
                }

                isDownloading = true;
            }
            else if ((match = RgxPlaylist.Match(e.Data)).Success)
            {
                var index = int.Parse(match.Groups[1].Value);
                progress?.Report(new DownloadProgress(DownloadState.PreProcessing, index: index));
                isDownloading = false;
            }
            else if (isDownloading && RgxPost.IsMatch(e.Data))
            {
                progress?.Report(new DownloadProgress(DownloadState.PostProcessing, 1));
                isDownloading = false;
            }

            Debug.WriteLine($"[yt-dlp] {e.Data}");
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

            Debug.WriteLine($"[yt-dlp ERROR] {args.Data}");
            progress?.Report(new DownloadProgress(DownloadState.Error, data: args.Data));
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
        ct.Register(() =>
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

        Debug.WriteLine($"[yt-dlp] Arguments: {process.StartInfo.Arguments}");
        if (!await Task.Run(() => process.Start()))
        {
            tcs.TrySetException(new InvalidOperationException("Failed to start yt-dlp process."));
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        progress?.Report(new DownloadProgress(DownloadState.PreProcessing));
        return await tcs.Task;
    }
}

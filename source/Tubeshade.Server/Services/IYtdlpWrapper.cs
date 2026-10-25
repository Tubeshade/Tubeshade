using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tubeshade.Data.Media.Videos;
using Tubeshade.Data.Preferences;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;

namespace Tubeshade.Server.Services;

public interface IYtdlpWrapper
{
    ValueTask<VideoData> FetchUnknownUrlData(
        string url,
        string? cookieFilepath,
        PlayerClient? client,
        string? infoJsonFilePath,
        CancellationToken cancellationToken);

    ValueTask<VideoData> FetchPlaylistEntryUrls(
        string playlistUrl,
        int? count,
        string? cookieFilepath,
        CancellationToken cancellationToken);

    ValueTask<RunResult<VideoData>> FetchVideoData(
        string videoUrl,
        string? cookieFilepath,
        PlayerClient? client,
        string? infoJsonFilePath,
        CancellationToken cancellationToken);

    ValueTask<VideoData> SelectVideoFormat(
        string infoJsonFilePath,
        string format,
        CancellationToken cancellationToken);

    ValueTask<FormatData[][]> SelectFormats(
        string videoUrl,
        IEnumerable<string> formats,
        string? cookieFilepath,
        PlayerClient? client,
        CancellationToken cancellationToken);

    ValueTask DownloadThumbnails(
        string url,
        string prefix,
        string path,
        string? cookieFilepath,
        CancellationToken cancellationToken);

    ValueTask DownloadThumbnails(
        string infoJsonFilePath,
        string prefix,
        string path,
        CancellationToken cancellationToken);

    Task<RunResult<string>> DownloadVideo(
        string videoUrl,
        string format,
        VideoContainerType containerType,
        string outputFolder,
        string outputTemplate,
        string? cookieFilepath,
        long? limitRate,
        PlayerClient? client,
        CancellationToken cancellationToken);

    OptionSet GetDownloadFormatArgs(
        string format,
        string output,
        string? cookieFilepath,
        long? limitRate,
        PlayerClient? client);
}

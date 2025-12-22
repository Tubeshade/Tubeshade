using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tubeshade.Data.Media;
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
        CancellationToken cancellationToken);

    ValueTask<VideoData> FetchPlaylistEntryUrls(
        string playlistUrl,
        int? count,
        string? cookieFilepath,
        CancellationToken cancellationToken);

    ValueTask<RunResult<VideoData>> FetchVideoData(
        string videoUrl,
        string? cookieFilepath,
        CancellationToken cancellationToken);

    ValueTask<VideoData> FetchVideoFormatData(
        string videoUrl,
        string format,
        string? cookieFilepath,
        PlayerClient? client,
        bool ignoreNoFormatsError,
        CancellationToken cancellationToken);

    ValueTask<FormatData[][]> SelectFormats(string videoUrl,
        IEnumerable<string> formats,
        string? cookieFilepath,
        PlayerClient? client,
        CancellationToken cancellationToken);

    ValueTask DownloadThumbnail(
        string thumbnailUrl,
        string path,
        string fileNameWithoutExtension,
        string? cookieFilepath,
        CancellationToken cancellationToken);

    ValueTask DownloadChannelThumbnails(
        string channelUrl,
        string path,
        string? cookieFilepath,
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

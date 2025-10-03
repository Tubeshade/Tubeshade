using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tubeshade.Data.Preferences;
using Tubeshade.Server.Services;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

namespace Tubeshade.Server.Tests.Integration;

public sealed class MockYtdlpWrapper : IYtdlpWrapper
{
    /// <inheritdoc />
    public ValueTask<VideoData> FetchUnknownUrlData(
        string url,
        string? cookieFilepath,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public ValueTask<VideoData> FetchPlaylistEntryUrls(
        string playlistUrl,
        int? count,
        string? cookieFilepath,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public ValueTask<RunResult<VideoData>> FetchVideoData(
        string videoUrl,
        string? cookieFilepath,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public ValueTask<VideoData> FetchVideoFormatData(
        string videoUrl,
        string format,
        string? cookieFilepath,
        PlayerClient? client,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public ValueTask<(string SelectedFormat, FormatData[] Formats)[]> SelectFormats(
        string videoUrl,
        IEnumerable<string> formats,
        string? cookieFilepath,
        PlayerClient? client,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public ValueTask DownloadThumbnail(
        string thumbnailUrl,
        string path,
        string? cookieFilepath,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }
}

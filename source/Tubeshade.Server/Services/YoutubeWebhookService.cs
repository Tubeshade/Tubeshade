using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Npgsql;
using PubSubHubbub.Models;
using Tubeshade.Data;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Media;
using Tubeshade.Data.Preferences;
using Tubeshade.Data.Tasks;
using YoutubeDLSharp.Metadata;

namespace Tubeshade.Server.Services;

public sealed class YoutubeWebhookService
{
    private static readonly XmlSerializer FeedSerializer = new(typeof(Feed));

    private readonly ILogger<YoutubeWebhookService> _logger;
    private readonly NpgsqlConnection _connection;
    private readonly VideoRepository _videoRepository;
    private readonly PreferencesRepository _preferencesRepository;
    private readonly TaskService _taskService;
    private readonly IYtdlpWrapper _ytdlpWrapper;
    private readonly YoutubePostChecker _postChecker;

    public YoutubeWebhookService(
        ILogger<YoutubeWebhookService> logger,
        NpgsqlConnection connection,
        VideoRepository videoRepository,
        PreferencesRepository preferencesRepository,
        TaskService taskService,
        IYtdlpWrapper ytdlpWrapper,
        YoutubePostChecker postChecker)
    {
        _logger = logger;
        _connection = connection;
        _videoRepository = videoRepository;
        _preferencesRepository = preferencesRepository;
        _taskService = taskService;
        _ytdlpWrapper = ytdlpWrapper;
        _postChecker = postChecker;
    }

    public async ValueTask ProcessWebhook(
        string payload,
        Guid libraryId,
        Guid userId,
        Guid channelId,
        CookiesService cookiesService,
        CancellationToken cancellationToken)
    {
        using var reader = new StringReader(payload);
        var feed = (Feed)FeedSerializer.Deserialize(reader)!;

        var videoUrl = feed switch
        {
            { Entry.Link.Uri: { } uri } => uri,
            { DeletedEntry.Link.Uri: { } deletedUri } => deletedUri,
            _ => throw new InvalidOperationException("Feed update does not contain a link to a video"),
        };

        await using var transaction = await _connection.OpenAndBeginTransaction(IsolationLevel.RepeatableRead, cancellationToken);

        var preferences = await _preferencesRepository.GetEffectiveForChannel(libraryId, channelId, userId, transaction, cancellationToken);
        var existingVideo = await _videoRepository.FindByExternalUrl(videoUrl, userId, Access.Read, transaction);

        await transaction.CommitAsync(cancellationToken);

        // always index existing videos
        if (existingVideo is not null)
        {
            await _taskService.IndexVideo(userId, libraryId, existingVideo, TaskSource.Webhook, cancellationToken);
            return;
        }

        preferences ??= new();
        preferences.ApplyDefaults();

        _ = VideoType.TryFromUrl(videoUrl, out var type);

        // shorts can be identified from url
        if (type == VideoType.Short)
        {
            if (preferences.ShortsCount is <= 0)
            {
                _logger.FeedUpdateIgnored(type.Name);
            }
            else
            {
                await _taskService.IndexVideo(userId, libraryId, videoUrl, channelId, TaskSource.Webhook, cancellationToken);
            }

            return;
        }

        if (await _postChecker.IsYouTubePost(videoUrl, cancellationToken))
        {
            _logger.SkippingPost();
            return;
        }

        // posts cannot be identified immediately after receiving the notification, retry a moment later
        await Task.Delay(5_000, cancellationToken);
        if (await _postChecker.IsYouTubePost(videoUrl, cancellationToken))
        {
            _logger.SkippingPost();
            return;
        }

        switch (preferences)
        {
            // can index if all remaining video types need to be indexed
            case { VideosCount: > 0 or null, LiveStreamsCount: > 0 or null }:
                await _taskService.IndexVideo(userId, libraryId, videoUrl, channelId, TaskSource.Webhook, cancellationToken);
                return;

            case { VideosCount: < 1, LiveStreamsCount: < 1 }:
                _logger.FeedUpdateIgnored();
                return;
        }

        // need to invoke yt-dlp because livestreams cannot be identified directly from url
        var cookieFilepath = await cookiesService.RefreshCookieFile();
        var videoData = await _ytdlpWrapper.FetchUnknownUrlData(videoUrl, cookieFilepath, cancellationToken);
        if (videoData.ResultType is not MetadataType.Video)
        {
            throw new InvalidOperationException($"Unexpected yt-dlp result type {videoData.ResultType}");
        }

        type ??= videoData.GetVideoType();

        if ((type.Name is VideoType.Names.Video && preferences.VideosCount is not > 0) ||
            (type.Name is VideoType.Names.Livestream && preferences.LiveStreamsCount is not > 0))
        {
            _logger.FeedUpdateIgnored(type.Name);
            return;
        }

        await _taskService.IndexVideo(userId, libraryId, videoUrl, channelId, TaskSource.Webhook, cancellationToken);
    }
}

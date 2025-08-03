using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Data.Preferences;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Libraries;

public sealed class Video : LibraryPageBase
{
    private readonly VideoRepository _videoRepository;
    private readonly ChannelRepository _channelRepository;
    private readonly LibraryRepository _libraryRepository;
    private readonly PreferencesRepository _preferencesRepository;
    private readonly SponsorBlockSegmentRepository _sponsorBlockSegmentRepository;
    private readonly NpgsqlConnection _connection;

    public Video(
        VideoRepository videoRepository,
        ChannelRepository channelRepository,
        LibraryRepository libraryRepository,
        PreferencesRepository preferencesRepository,
        SponsorBlockSegmentRepository sponsorBlockSegmentRepository,
        NpgsqlConnection connection)
    {
        _videoRepository = videoRepository;
        _channelRepository = channelRepository;
        _libraryRepository = libraryRepository;
        _preferencesRepository = preferencesRepository;
        _sponsorBlockSegmentRepository = sponsorBlockSegmentRepository;
        _connection = connection;
    }

    [BindProperty(SupportsGet = true)]
    public Guid VideoId { get; set; }

    public VideoEntity Entity { get; set; } = null!;

    public List<VideoFileEntity> Files { get; set; } = [];

    public ChannelEntity Channel { get; set; } = null!;

    public LibraryEntity Library { get; set; } = null!;

    public decimal PlaybackSpeed { get; set; } = 1.0m;

    public bool HasSubtitles { get; set; }

    public bool HasChapters { get; set; }

    public bool HasSponsorBlockSegments { get; set; }

    public async Task OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        Entity = await _videoRepository.GetAsync(VideoId, userId, cancellationToken);
        Files = await _videoRepository.GetFilesAsync(VideoId, userId, cancellationToken);

        Channel = await _channelRepository.GetAsync(Entity.ChannelId, userId, cancellationToken);
        Library = await _libraryRepository.GetAsync(LibraryId, userId, cancellationToken);

        var preferences = await _preferencesRepository.GetEffectiveForVideo(LibraryId, VideoId, userId, cancellationToken);
        if (preferences?.PlaybackSpeed is { } playbackSpeed)
        {
            PlaybackSpeed = playbackSpeed;
        }

        HasSubtitles = System.IO.File.Exists(Entity.GetSubtitlesFilePath());
        HasChapters = System.IO.File.Exists(Entity.GetChaptersFilePath());

        var segments = await _sponsorBlockSegmentRepository.GetForVideo(VideoId, userId, cancellationToken);
        HasSponsorBlockSegments = segments is not [];
    }

    public async Task<IActionResult> OnPostViewed(string? viewed, Guid videoId)
    {
        var userId = User.GetUserId();
        await using var transaction = await _connection.OpenAndBeginTransaction();
        if (viewed is not null)
        {
            await _videoRepository.MarkAsWatched(videoId, userId, transaction);
        }
        else
        {
            await _videoRepository.MarkAsNotWatched(videoId, userId, transaction);
        }

        await transaction.CommitAsync();
        return StatusCode(StatusCodes.Status200OK);
    }
}

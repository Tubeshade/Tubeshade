using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Tubeshade.Data.Media;
using Tubeshade.Data.Preferences;
using Tubeshade.Server.Configuration.Auth;

namespace Tubeshade.Server.Pages.Libraries;

public sealed class Video : LibraryPageBase
{
    private readonly VideoRepository _videoRepository;
    private readonly ChannelRepository _channelRepository;
    private readonly LibraryRepository _libraryRepository;
    private readonly PreferencesRepository _preferencesRepository;

    public Video(
        VideoRepository videoRepository,
        ChannelRepository channelRepository,
        LibraryRepository libraryRepository,
        PreferencesRepository preferencesRepository)
    {
        _videoRepository = videoRepository;
        _channelRepository = channelRepository;
        _libraryRepository = libraryRepository;
        _preferencesRepository = preferencesRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid VideoId { get; set; }

    public VideoEntity Entity { get; set; } = null!;

    public List<VideoFileEntity> Files { get; set; } = [];

    public ChannelEntity Channel { get; set; } = null!;

    public LibraryEntity Library { get; set; } = null!;

    public decimal PlaybackSpeed { get; set; } = 1.0m;

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
    }
}

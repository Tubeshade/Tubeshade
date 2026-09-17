using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Htmx;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Data.Media.Channels;
using Tubeshade.Data.Media.Creators;
using Tubeshade.Data.Media.Playlists;
using Tubeshade.Data.Media.Videos;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Shared;
using Tubeshade.Server.Pages.Videos;
using Tubeshade.Server.Services;

namespace Tubeshade.Server.Pages.Playlists;

public sealed class Playlist : PageModel, IPlaylistVideoPage, INonLibraryPage
{
    private readonly NpgsqlConnection _connection;
    private readonly PlaylistRepository _repository;
    private readonly LibraryRepository _libraryRepository;
    private readonly ChannelRepository _channelRepository;
    private readonly PlaylistService _playlistService;
    private readonly VideoRepository _videoRepository;
    private readonly SponsorBlockSegmentRepository _segmentRepository;
    private readonly CreatorRepository _creatorRepository;

    public Playlist(
        NpgsqlConnection connection,
        PlaylistRepository repository,
        LibraryRepository libraryRepository,
        ChannelRepository channelRepository,
        PlaylistService playlistService,
        VideoRepository videoRepository,
        SponsorBlockSegmentRepository segmentRepository,
        CreatorRepository creatorRepository)
    {
        _connection = connection;
        _repository = repository;
        _libraryRepository = libraryRepository;
        _channelRepository = channelRepository;
        _playlistService = playlistService;
        _videoRepository = videoRepository;
        _segmentRepository = segmentRepository;
        _creatorRepository = creatorRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid PlaylistId { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public int? PageSize { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public int? PageIndex { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public ViewStatus? Viewed { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public VideoType? Type { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public bool? WithFiles { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public ExternalAvailability? Availability { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public Guid? CreatorId { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public SortVideoBy? SortBy { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public SortDirection? SortDirection { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<CreatorEntity> Creators { get; private set; } = [];

    /// <inheritdoc />
    public IReadOnlyList<LibraryEntity> Libraries { get; private set; } = [];

    /// <inheritdoc />
    public PaginatedData<VideoModel> PageData { get; private set; } = null!;

    public PlaylistEntity Entity { get; private set; } = null!;

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var parameters = this.GetVideoParameters(userId, PlaylistId);

        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);

        Libraries = await _libraryRepository.GetAsync(userId, transaction);
        Entity = await _repository.GetAsync(PlaylistId, userId, transaction);
        Creators = await _creatorRepository.GetFiltered(
            new CreatorParameters
            {
                Limit = int.MaxValue,
                Offset = 0,
                UserId = userId,
                SortBy = SortCreatorBy.CreatorName,
                SortDirection = SortDirection.Ascending,
            },
            transaction,
            cancellationToken);

        var videos = await _videoRepository.GetFilteredDetailed(parameters, cancellationToken);
        var channels = await _channelRepository.GetAsync(userId, transaction);

        var videoIds = videos.Select(video => video.Id).ToArray();
        var segments = await _segmentRepository.GetForVideos(videoIds, userId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        var models = videos.MapToModels(segments, channels).ToList();
        var totalCount = videos is [var first, ..] ? first.TotalCount : 0;

        PageData = new PaginatedData<VideoModel>
        {
            LibraryId = null,
            Data = models,
            Page = PageIndex ?? Defaults.PageIndex,
            PageSize = parameters.Limit,
            TotalCount = totalCount,
        };

        return Request.IsHtmx()
            ? Partial("Videos/_FilteredVideos", this)
            : Page();
    }

    /// <inheritdoc />
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

    public async Task<IActionResult> OnPostSubscribe()
    {
        var playlist = await _playlistService.Subscribe(PlaylistId, User.GetUserId());
        return Partial("Playlists/_SubscribePlaylist", playlist);
    }

    public async Task<IActionResult> OnPostUnsubscribe()
    {
        var playlist = await _playlistService.Unsubscribe(PlaylistId, User.GetUserId());
        return Partial("Playlists/_SubscribePlaylist", playlist);
    }
}

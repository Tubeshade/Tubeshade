using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Htmx;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using SponsorBlock;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Data.Preferences;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Libraries;

public sealed class Library : LibraryPageBase, IPaginatedDataPage<VideoModel>
{
    private readonly NpgsqlConnection _connection;
    private readonly LibraryRepository _repository;
    private readonly VideoRepository _videoRepository;
    private readonly ChannelRepository _channelRepository;
    private readonly PreferencesRepository _preferencesRepository;
    private readonly SponsorBlockSegmentRepository _segmentRepository;

    public Library(
        LibraryRepository repository,
        VideoRepository videoRepository,
        NpgsqlConnection connection,
        ChannelRepository channelRepository,
        PreferencesRepository preferencesRepository,
        SponsorBlockSegmentRepository segmentRepository)
    {
        _repository = repository;
        _videoRepository = videoRepository;
        _connection = connection;
        _channelRepository = channelRepository;
        _preferencesRepository = preferencesRepository;
        _segmentRepository = segmentRepository;
    }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public int? PageSize { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public int? PageIndex { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Tab { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    /// <inheritdoc />
    public PaginatedData<VideoModel> PageData { get; set; } = null!;

    public LibraryEntity Entity { get; set; } = null!;

    [BindProperty]
    public UpdatePreferencesModel UpdatePreferencesModel { get; set; } = new();

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var pageSize = PageSize ?? Defaults.PageSize;
        var page = PageIndex ?? Defaults.PageIndex;
        var offset = pageSize * page;

        Entity = await _repository.GetAsync(LibraryId, userId, cancellationToken);
        var videos = Query is null
            ? await _videoRepository.GetForLibrary(LibraryId, userId, pageSize, offset, cancellationToken)
            : await _videoRepository.GetForLibrary(LibraryId, userId, pageSize, offset, Query, cancellationToken);
        var channels = await _channelRepository.GetAsync(userId, cancellationToken);

        var videoIds = videos.Select(video => video.Id).ToArray();
        var segments = await _segmentRepository.GetForVideos(videoIds, userId, cancellationToken);

        var models = videos.Select(video =>
        {
            var skippedDuration = segments
                .Where(segment => segment.VideoId == video.Id && segment.Category != SegmentCategory.Filler)
                .GetTotalDuration();

            var actualDuration = (video.Duration - skippedDuration).Normalize();

            return new VideoModel
            {
                Video = video,
                ActualDuration = actualDuration,
                Channel = channels.Single(channel => video.ChannelId == channel.Id), // todo
            };
        }).ToList();

        var totalCount = videos is [] ? 0 : videos[0].TotalCount;

        PageData = new PaginatedData<VideoModel>
        {
            LibraryId = LibraryId,
            Data = models,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };

        var preferences = await _preferencesRepository.FindForLibrary(LibraryId, userId, cancellationToken);
        UpdatePreferencesModel = new UpdatePreferencesModel
        {
            PlaybackSpeed = preferences?.PlaybackSpeed,
            VideosCount = preferences?.VideosCount,
            LiveStreamsCount = preferences?.LiveStreamsCount,
            ShortsCount = preferences?.ShortsCount,
        };

        return Request.IsHtmx()
            ? Partial("_LibraryTabs", this)
            : Page();
    }

    public async Task<IActionResult> OnPostUpdatePreferences()
    {
        if (!ModelState.IsValid)
        {
            await OnGet(CancellationToken.None);
            return Page();
        }

        var userId = User.GetUserId();
        var cancellationToken = CancellationToken.None;

        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var preferences = await _preferencesRepository.FindForLibrary(LibraryId, userId, transaction);
        if (preferences is null)
        {
            var id = await _preferencesRepository.AddAsync(
                new PreferencesEntity
                {
                    CreatedByUserId = userId,
                    ModifiedByUserId = userId,
                    PlaybackSpeed = UpdatePreferencesModel.PlaybackSpeed,
                    VideosCount = UpdatePreferencesModel.VideosCount,
                    LiveStreamsCount = UpdatePreferencesModel.LiveStreamsCount,
                    ShortsCount = UpdatePreferencesModel.ShortsCount,
                    SubscriptionScheduleId = null
                },
                transaction);

            Trace.Assert(id is not null);

            var count = await _preferencesRepository.LinkToLibrary(id.Value, LibraryId, userId, transaction);

            Trace.Assert(count is 1);
        }
        else
        {
            preferences.PlaybackSpeed = UpdatePreferencesModel.PlaybackSpeed;
            preferences.VideosCount = UpdatePreferencesModel.VideosCount;
            preferences.LiveStreamsCount = UpdatePreferencesModel.LiveStreamsCount;
            preferences.ShortsCount = UpdatePreferencesModel.ShortsCount;

            var count = await _preferencesRepository.UpdateAsync(
                preferences,
                transaction);

            Trace.Assert(count is 1);
        }

        await transaction.CommitAsync(cancellationToken);

        return RedirectToPage();
    }
}

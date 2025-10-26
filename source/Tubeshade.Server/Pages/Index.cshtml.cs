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
using SponsorBlock;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Shared;
using Tubeshade.Server.Pages.Videos;

namespace Tubeshade.Server.Pages;

public sealed class IndexModel : PageModel, IVideoPage, INonLibraryPage
{
    private readonly NpgsqlConnection _connection;
    private readonly VideoRepository _videoRepository;
    private readonly ChannelRepository _channelRepository;
    private readonly LibraryRepository _libraryRepository;
    private readonly SponsorBlockSegmentRepository _segmentRepository;

    public IndexModel(
        NpgsqlConnection connection,
        VideoRepository videoRepository,
        ChannelRepository channelRepository,
        LibraryRepository libraryRepository,
        SponsorBlockSegmentRepository segmentRepository)
    {
        _connection = connection;
        _videoRepository = videoRepository;
        _channelRepository = channelRepository;
        _libraryRepository = libraryRepository;
        _segmentRepository = segmentRepository;
    }

    /// <inheritdoc />
    public int? PageSize { get; set; }

    /// <inheritdoc />
    public int? PageIndex { get; set; }

    /// <inheritdoc />
    public bool? Viewed { get; set; }

    /// <inheritdoc />
    public string? Query { get; set; }

    /// <inheritdoc />
    public VideoType? Type { get; set; }

    /// <inheritdoc />
    public bool? WithFiles { get; set; }

    /// <inheritdoc />
    public ExternalAvailability? Availability { get; set; }

    /// <inheritdoc />
    public PaginatedData<VideoModel> PageData { get; set; } = null!;

    /// <inheritdoc />
    public IEnumerable<LibraryEntity> Libraries { get; private set; } = null!;

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        if (WithFiles is null && !Request.Query.ContainsKey(nameof(WithFiles)))
        {
            WithFiles = true;
        }

        var userId = User.GetUserId();

        Libraries = await _libraryRepository.GetAsync(userId, cancellationToken);

        var pageSize = PageSize ?? Defaults.PageSize;
        var page = PageIndex ?? Defaults.PageIndex;
        var offset = pageSize * page;
        var videos = await _videoRepository.GetFiltered(
            new VideoParameters
            {
                UserId = userId,
                LibraryId = null,
                ChannelId = null,
                Limit = pageSize,
                Offset = offset,
                Viewed = Viewed,
                Query = Query,
                Type = Type,
                WithFiles = WithFiles,
                Availability = Availability,
            },
            cancellationToken);

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
            LibraryId = null,
            Data = models,
            Page = page,
            PageSize = pageSize,
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
}

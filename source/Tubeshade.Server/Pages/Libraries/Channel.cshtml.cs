using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Libraries;

public sealed class Channel : LibraryPageBase, IPaginatedDataPage<VideoModel>
{
    private readonly NpgsqlConnection _connection;
    private readonly LibraryRepository _libraryRepository;
    private readonly ChannelRepository _channelRepository;
    private readonly VideoRepository _videoRepository;
    private readonly IClock _clock;

    public Channel(
        ChannelRepository channelRepository,
        VideoRepository videoRepository,
        LibraryRepository libraryRepository,
        NpgsqlConnection connection,
        IClock clock)
    {
        _channelRepository = channelRepository;
        _videoRepository = videoRepository;
        _libraryRepository = libraryRepository;
        _connection = connection;
        _clock = clock;
    }

    [BindProperty(SupportsGet = true)]
    public Guid ChannelId { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public int? PageSize { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public int? PageIndex { get; set; }

    public LibraryEntity Library { get; set; } = null!;

    /// <inheritdoc />
    public PaginatedData<VideoModel> PageData { get; set; } = null!;

    public ChannelEntity Entity { get; set; } = null!;

    public async Task OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var pageSize = PageSize ?? 20;
        var page = PageIndex ?? 0;
        var offset = pageSize * page;

        Library = await _libraryRepository.GetAsync(LibraryId, userId, cancellationToken);
        Entity = await _channelRepository.GetAsync(ChannelId, userId, cancellationToken);
        var videos = await _videoRepository.GetChannelVideosAsync(
            ChannelId,
            userId,
            pageSize,
            offset,
            cancellationToken);

        var channels = await _channelRepository.GetAsync(userId, cancellationToken);
        var models = videos.Select(video => new VideoModel
        {
            Video = video,
            Channel = channels.Single(channel => video.ChannelId == channel.Id), // todo
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
    }

    public async Task<IActionResult> OnPostUpdatePreferences(UpdatePreferencesModel model)
    {
        if (!ModelState.IsValid)
        {
            await OnGet(CancellationToken.None);
            return Page();
        }

        await using var transaction = await _connection.OpenAndBeginTransaction();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUnsubscribe()
    {
        return await ChangeSubscribedAt(null);
    }

    public async Task<IActionResult> OnPostSubscribe()
    {
        return await ChangeSubscribedAt(_clock.GetCurrentInstant());
    }

    private async Task<IActionResult> ChangeSubscribedAt(Instant? subscribedAt)
    {
        var userId = User.GetUserId();

        await using var transaction = await _connection.OpenAndBeginTransaction();
        var channel = await _channelRepository.GetAsync(ChannelId, userId, transaction);

        channel.SubscribedAt = subscribedAt;
        channel.ModifiedByUserId = userId;
        channel.ModifiedAt = subscribedAt ?? _clock.GetCurrentInstant();

        var count = await _channelRepository.UpdateAsync(channel, transaction);
        Trace.Assert(count is not 0);

        await transaction.CommitAsync();
        return Partial("_Subscribe", channel);
    }
}

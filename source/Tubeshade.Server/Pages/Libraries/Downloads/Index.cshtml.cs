using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Media;
using Tubeshade.Data.Tasks;
using Tubeshade.Data.Tasks.Payloads;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Shared;

namespace Tubeshade.Server.Pages.Libraries.Downloads;

public sealed class Index : LibraryPageBase, IPaginatedDataPage<VideoModel>
{
    private readonly NpgsqlConnection _connection;
    private readonly LibraryRepository _libraryRepository;
    private readonly TaskRepository _taskRepository;
    private readonly VideoRepository _videoRepository;
    private readonly ChannelRepository _channelRepository;
    private readonly IClock _clock;

    public Index(
        NpgsqlConnection connection,
        LibraryRepository libraryRepository,
        TaskRepository taskRepository,
        VideoRepository videoRepository,
        ChannelRepository channelRepository,
        IClock clock)
    {
        _connection = connection;
        _libraryRepository = libraryRepository;
        _taskRepository = taskRepository;
        _videoRepository = videoRepository;
        _channelRepository = channelRepository;
        _clock = clock;
    }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public int? PageSize { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public int? PageIndex { get; set; }

    /// <inheritdoc />
    public PaginatedData<VideoModel> PageData { get; set; } = null!;

    public LibraryEntity Library { get; set; } = null!;

    [BindProperty(SupportsGet = true)]
    public Guid? ChannelId { get; set; }

    public List<ChannelEntity> Channels { get; set; } = [];

    public async Task OnGet(CancellationToken cancellationToken)
    {
        var userId = await OnGetCore(cancellationToken);
        Library = await _libraryRepository.GetAsync(LibraryId, userId, cancellationToken);
    }

    public async Task<IActionResult> OnGetDownloadable(CancellationToken cancellationToken)
    {
        _ = await OnGetCore(cancellationToken);
        return Partial("_DownloadableVideos", this);
    }

    public async Task<IActionResult> OnPostStartDownload(Guid videoId)
    {
        var userId = User.GetUserId();
        var cancellationToken = CancellationToken.None;

        var payload = new DownloadVideoPayload { LibraryId = LibraryId, VideoId = videoId, UserId = userId };

        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var taskId = await _taskRepository.AddDownloadTask(payload, userId, transaction);
        await _taskRepository.TriggerTask(taskId, transaction);
        await transaction.CommitAsync(cancellationToken);

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostIgnore(Guid videoId)
    {
        var userId = User.GetUserId();
        var cancellationToken = CancellationToken.None;

        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var video = await _videoRepository.FindAsync(videoId, userId, Access.Modify, transaction);
        if (video is null)
        {
            return NotFound();
        }

        video.IgnoredAt = _clock.GetCurrentInstant();
        video.IgnoredByUserId = userId;
        await _videoRepository.UpdateAsync(video, transaction);

        await transaction.CommitAsync(cancellationToken);
        return StatusCode(StatusCodes.Status200OK);
    }

    public async Task<IActionResult> OnPost(DownloadVideoModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await OnGet(cancellationToken);
            return Page();
        }

        var userId = User.GetUserId();
        cancellationToken = CancellationToken.None;

        var payload = new IndexPayload { Url = model.Url, LibraryId = LibraryId, UserId = userId };

        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
        var taskId = await _taskRepository.AddIndexTask(payload, userId, transaction);
        await _taskRepository.TriggerTask(taskId, transaction);
        await transaction.CommitAsync(cancellationToken);

        return RedirectToPage();
    }

    private async ValueTask<Guid> OnGetCore(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        Channels = await _channelRepository.GetForLibrary(LibraryId, userId, cancellationToken);

        var pageSize = PageSize ?? Defaults.PageSize;
        var page = PageIndex ?? Defaults.PageIndex;
        var offset = pageSize * page;
        var videos = ChannelId is { } channelId
            ? await _videoRepository.GetDownloadableVideos(LibraryId, channelId, userId, pageSize, offset, cancellationToken)
            : await _videoRepository.GetDownloadableVideos(LibraryId, userId, pageSize, offset, cancellationToken);

        var models = videos.Select(video => new VideoModel
        {
            Video = video,
            Channel = Channels.Single(channel => video.ChannelId == channel.Id), // todo
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

        return userId;
    }
}

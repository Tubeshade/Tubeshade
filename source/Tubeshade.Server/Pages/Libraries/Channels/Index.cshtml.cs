using System;
using System.Threading;
using System.Threading.Tasks;
using Htmx;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Media;
using Tubeshade.Data.Media.Channels;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Channels;
using Tubeshade.Server.Pages.Shared;
using Tubeshade.Server.Services;

namespace Tubeshade.Server.Pages.Libraries.Channels;

public sealed class Index : LibraryPageBase, IChannelPage
{
    private readonly NpgsqlConnection _connection;
    private readonly ChannelRepository _channelRepository;
    private readonly LibraryRepository _libraryRepository;
    private readonly SubscriptionsService _subscriptionsService;
    private readonly TaskRepository _taskRepository;

    public Index(
        ChannelRepository channelRepository,
        LibraryRepository libraryRepository,
        NpgsqlConnection connection,
        SubscriptionsService subscriptionsService,
        TaskRepository taskRepository)
    {
        _channelRepository = channelRepository;
        _libraryRepository = libraryRepository;
        _connection = connection;
        _subscriptionsService = subscriptionsService;
        _taskRepository = taskRepository;
    }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public string? Query { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public ExternalAvailability? Availability { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public SortChannelBy? SortBy { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public SortDirection? SortDirection { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public int? PageSize { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public int? PageIndex { get; set; }

    /// <inheritdoc />
    public PaginatedData<DetailedChannel> PageData { get; private set; } = null!;

    public LibraryEntity Library { get; private set; } = null!;

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var parameters = this.GetChannelParameters(userId, LibraryId);

        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);

        Library = await _libraryRepository.GetAsync(LibraryId, userId, transaction);
        var channels = await _channelRepository.GetFiltered(parameters, transaction, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        var totalCount = channels is [] ? 0 : channels[0].TotalCount;
        PageData = new PaginatedData<DetailedChannel>
        {
            LibraryId = LibraryId,
            Data = channels,
            Page = PageIndex ?? Defaults.PageIndex,
            PageSize = parameters.Limit,
            TotalCount = totalCount,
        };

        return Request.IsHtmx()
            ? Partial("Channels/_FilteredChannels", this)
            : Page();
    }

    /// <inheritdoc />
    public async Task<IActionResult> OnPostSubscribe(Guid channelId)
    {
        var userId = User.GetUserId();
        _ = await _subscriptionsService.Subscribe(channelId, userId);

        var channel = await _channelRepository.GetDetailed(channelId, userId);
        return Partial("Channels/_ChannelCard", new ChannelModel(channel, LibraryId));
    }

    /// <inheritdoc />
    public async Task<IActionResult> OnPostUnsubscribe(Guid channelId)
    {
        var userId = User.GetUserId();
        _ = await _subscriptionsService.Unsubscribe(channelId, userId);

        var channel = await _channelRepository.GetDetailed(channelId, userId);
        return Partial("Channels/_ChannelCard", new ChannelModel(channel, LibraryId));
    }

    /// <inheritdoc />
    public async Task<IActionResult> OnPostScan(Guid channelId, bool? all)
    {
        var userId = User.GetUserId();
        var cancellationToken = CancellationToken.None;

        await using (var transaction = await _connection.OpenAndBeginTransaction(cancellationToken))
        {
            var task = TaskEntity.ScanChannel(LibraryId, userId, channelId, all ?? false);
            var taskId = await _taskRepository.AddTask(task, transaction);
            await _taskRepository.TriggerTask(taskId, TaskSource.User, userId, transaction);

            await transaction.CommitAsync(cancellationToken);
        }

        return StatusCode(StatusCodes.Status204NoContent);
    }
}

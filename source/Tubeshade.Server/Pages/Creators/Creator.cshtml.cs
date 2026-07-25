using System;
using System.Collections.Generic;
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
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Channels;
using Tubeshade.Server.Pages.Shared;
using Tubeshade.Server.Services;

namespace Tubeshade.Server.Pages.Creators;

public sealed class Creator : PageModel, IChannelPage, INonLibraryPage
{
    private readonly NpgsqlConnection _connection;
    private readonly CreatorRepository _repository;
    private readonly LibraryRepository _libraryRepository;
    private readonly ChannelRepository _channelRepository;
    private readonly SubscriptionsService _subscriptionsService;
    private readonly TaskRepository _taskRepository;

    public Creator(
        NpgsqlConnection connection,
        CreatorRepository repository,
        LibraryRepository libraryRepository,
        ChannelRepository channelRepository,
        SubscriptionsService subscriptionsService,
        TaskRepository taskRepository)
    {
        _connection = connection;
        _repository = repository;
        _libraryRepository = libraryRepository;
        _channelRepository = channelRepository;
        _subscriptionsService = subscriptionsService;
        _taskRepository = taskRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid CreatorId { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public int? PageSize { get; set; }

    /// <inheritdoc />
    [BindProperty(SupportsGet = true)]
    public int? PageIndex { get; set; }

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
    public PaginatedData<DetailedChannel> PageData { get; private set; } = null!;

    /// <inheritdoc />
    public List<LibraryEntity> Libraries { get; private set; } = [];

    public CreatorEntity Entity { get; private set; } = null!;

    public async Task<IActionResult> OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var parameters = this.GetChannelParameters(userId, null);
        parameters.CreatorId = CreatorId;

        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);

        Libraries = await _libraryRepository.GetAsync(userId, transaction);
        Entity = await _repository.GetAsync(CreatorId, userId, transaction);
        var channels = await _channelRepository.GetFiltered(parameters, transaction, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        var totalCount = channels is [var first, ..] ? first.TotalCount : 0;
        PageData = new PaginatedData<DetailedChannel>
        {
            LibraryId = null,
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
        return Partial("Channels/_ChannelCard", new ChannelModel(channel, null));
    }

    /// <inheritdoc />
    public async Task<IActionResult> OnPostUnsubscribe(Guid channelId)
    {
        var userId = User.GetUserId();
        _ = await _subscriptionsService.Unsubscribe(channelId, userId);

        var channel = await _channelRepository.GetDetailed(channelId, userId);
        return Partial("Channels/_ChannelCard", new ChannelModel(channel, null));
    }

    /// <inheritdoc />
    public async Task<IActionResult> OnPostScan(Guid channelId, bool? all)
    {
        var userId = User.GetUserId();
        var cancellationToken = CancellationToken.None;

        await using (var transaction = await _connection.OpenAndBeginTransaction(cancellationToken))
        {
            var libraryId = await _channelRepository.GetPrimaryLibraryId(channelId, transaction, cancellationToken);
            var task = TaskEntity.ScanChannel(libraryId, userId, channelId, all ?? false);
            var taskId = await _taskRepository.AddTask(task, transaction);
            await _taskRepository.TriggerTask(taskId, TaskSource.User, userId, transaction);

            await transaction.CommitAsync(cancellationToken);
        }

        return StatusCode(StatusCodes.Status204NoContent);
    }
}

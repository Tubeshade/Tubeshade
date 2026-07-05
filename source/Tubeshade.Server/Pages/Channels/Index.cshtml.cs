using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Media;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Shared;
using Tubeshade.Server.Services;

namespace Tubeshade.Server.Pages.Channels;

public sealed class Index : PageModel, IChannelsPage, INonLibraryPage
{
    private readonly NpgsqlConnection _connection;
    private readonly ChannelRepository _channelRepository;
    private readonly LibraryRepository _libraryRepository;
    private readonly ImageFileRepository _imageRepository;
    private readonly SubscriptionsService _subscriptionsService;
    private readonly TaskRepository _taskRepository;

    public Index(
        NpgsqlConnection connection,
        ChannelRepository channelRepository,
        LibraryRepository libraryRepository,
        ImageFileRepository imageRepository,
        SubscriptionsService subscriptionsService,
        TaskRepository taskRepository)
    {
        _connection = connection;
        _channelRepository = channelRepository;
        _libraryRepository = libraryRepository;
        _imageRepository = imageRepository;
        _subscriptionsService = subscriptionsService;
        _taskRepository = taskRepository;
    }

    /// <inheritdoc />
    public List<LibraryEntity> Libraries { get; private set; } = [];

    /// <inheritdoc />
    public List<ChannelModel> Channels { get; private set; } = [];

    public async Task OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);

        Libraries = await _libraryRepository.GetAsync(userId, transaction);
        var channels = await _channelRepository.GetAsync(userId, transaction);

        Channels = new(channels.Count);
        foreach (var channel in channels)
        {
            // todo: avoid N+1 queries
            var model = await ToModel(channel, userId, transaction, cancellationToken);
            Channels.Add(model);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IActionResult> OnPostSubscribe(Guid channelId)
    {
        var userId = User.GetUserId();
        var channel = await _subscriptionsService.Subscribe(channelId, userId);

        await using var transaction = await _connection.OpenAndBeginTransaction();
        var model = await ToModel(channel, userId, transaction, CancellationToken.None);
        await transaction.CommitAsync();

        return Partial("Channels/_ChannelCard", model);
    }

    /// <inheritdoc />
    public async Task<IActionResult> OnPostUnsubscribe(Guid channelId)
    {
        var userId = User.GetUserId();
        var channel = await _subscriptionsService.Unsubscribe(channelId, userId);

        await using var transaction = await _connection.OpenAndBeginTransaction();
        var model = await ToModel(channel, userId, transaction, CancellationToken.None);
        await transaction.CommitAsync();

        return Partial("Channels/_ChannelCard", model);
    }

    /// <inheritdoc />
    public async Task<IActionResult> OnPostScan(Guid channelId, bool? all)
    {
        var userId = User.GetUserId();
        var cancellationToken = CancellationToken.None;

        await using (var transaction = await _connection.OpenAndBeginTransaction(cancellationToken))
        {
            var libraryId = await _channelRepository.GetPrimaryLibraryId(channelId, transaction);
            var task = TaskEntity.ScanChannel(libraryId, userId, channelId, all ?? false);
            var taskId = await _taskRepository.AddTask(task, transaction);
            await _taskRepository.TriggerTask(taskId, TaskSource.User, userId, transaction);

            await transaction.CommitAsync(cancellationToken);
        }

        return StatusCode(StatusCodes.Status204NoContent);
    }

    private async Task<ChannelModel> ToModel(
        ChannelEntity channel,
        Guid userId,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var libraryId = await _channelRepository.GetPrimaryLibraryId(channel.Id, transaction);
        var images = await _imageRepository.GetForChannel(channel.Id, userId, Access.Read, transaction, cancellationToken);

        return new()
        {
            LibraryId = libraryId,
            Channel = channel,
            Thumbnails = images.Where(image => image.Type == ImageType.Thumbnail).ToList(),
            Banners = images.Where(image => image.Type == ImageType.Banner).ToList(),
        };
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Media;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Configuration.Auth;
using Tubeshade.Server.Pages.Channels;
using Tubeshade.Server.Services;

namespace Tubeshade.Server.Pages.Libraries.Channels;

public sealed class Index : LibraryPageBase, IChannelsPage
{
    private readonly NpgsqlConnection _connection;
    private readonly ChannelRepository _channelRepository;
    private readonly LibraryRepository _libraryRepository;
    private readonly ImageFileRepository _imageRepository;
    private readonly SubscriptionsService _subscriptionsService;
    private readonly TaskRepository _taskRepository;

    public Index(
        ChannelRepository channelRepository,
        LibraryRepository libraryRepository,
        ImageFileRepository imageRepository,
        NpgsqlConnection connection,
        SubscriptionsService subscriptionsService,
        TaskRepository taskRepository)
    {
        _channelRepository = channelRepository;
        _libraryRepository = libraryRepository;
        _imageRepository = imageRepository;
        _connection = connection;
        _subscriptionsService = subscriptionsService;
        _taskRepository = taskRepository;
    }

    /// <inheritdoc />
    public List<ChannelModel> Channels { get; private set; } = [];

    public LibraryEntity Library { get; private set; } = null!;

    public async Task OnGet(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);

        Library = await _libraryRepository.GetAsync(LibraryId, userId, transaction);
        var channels = await _channelRepository.GetForLibrary(LibraryId, userId, cancellationToken); // todo

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
            var task = TaskEntity.ScanChannel(LibraryId, userId, channelId, all ?? false);
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
        var images = await _imageRepository.GetForChannel(channel.Id, userId, Access.Read, transaction, cancellationToken);
        return new()
        {
            LibraryId = LibraryId,
            Channel = channel,
            Thumbnails = images.Where(image => image.Type == ImageType.Thumbnail).ToList(),
            Banners = images.Where(image => image.Type == ImageType.Banner).ToList(),
        };
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Identity;
using Tubeshade.Data.Media;
using Tubeshade.Data.Tasks;
using static System.Data.IsolationLevel;

namespace Tubeshade.Server.Configuration.Startup.Migrations;

internal sealed class ChannelImagesMigration : IApplicationMigration
{
    private readonly NpgsqlConnection _connection;
    private readonly UserRepository _userRepository;
    private readonly ChannelRepository _channelRepository;
    private readonly ImageFileRepository _imageRepository;
    private readonly TaskRepository _taskRepository;

    public ChannelImagesMigration(
        NpgsqlConnection connection,
        UserRepository userRepository,
        ChannelRepository channelRepository,
        ImageFileRepository imageRepository,
        TaskRepository taskRepository)
    {
        _connection = connection;
        _userRepository = userRepository;
        _channelRepository = channelRepository;
        _imageRepository = imageRepository;
        _taskRepository = taskRepository;
    }

    /// <inheritdoc />
    public async ValueTask MigrateAsync(CancellationToken cancellationToken)
    {
        Guid userId;
        List<ChannelEntity> channels;

        await using (var transaction = await _connection.OpenAndBeginTransaction(ReadCommitted, cancellationToken))
        {
            userId = await _userRepository.GetSystemUserId(transaction);
            channels = await _channelRepository.GetAsync(userId, transaction);

            await transaction.CommitAsync(cancellationToken);
        }

        foreach (var channel in channels)
        {
            await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
            var images = await _imageRepository.GetForChannel(channel.Id, userId, Access.Read, transaction, cancellationToken);
            if (images is not [])
            {
                continue;
            }

            var libraryId = await _channelRepository.GetPrimaryLibraryId(channel.Id, transaction);
            var tasks = await _taskRepository.GetRunningTasks(
                new TaskParameters
                {
                    UserId = userId,
                    LibraryId = libraryId,
                    Type = TaskType.Index,
                    Url = channel.ExternalUrl,
                    Limit = 1,
                    Offset = 0,
                },
                transaction,
                cancellationToken);

            if (tasks is [])
            {
                var task = TaskEntity.Index(libraryId, userId, channel.Id, null, channel.ExternalUrl);
                if (await _taskRepository.TryAddTask(task, transaction) is { } taskId)
                {
                    await _taskRepository.TriggerTask(taskId, TaskSource.Schedule, transaction);
                }
            }

            await transaction.CommitAsync(cancellationToken);
        }
    }
}

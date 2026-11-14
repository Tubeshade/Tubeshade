using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Media;
using Tubeshade.Data.Media.Channels;
using Tubeshade.Data.Tasks;
using static System.Data.IsolationLevel;

namespace Tubeshade.Server.Configuration.Startup.Migrations;

internal sealed class ChannelImagesMigration : IApplicationMigration
{
    private readonly NpgsqlConnection _connection;
    private readonly OwnerRepository _ownerRepository;
    private readonly ChannelRepository _channelRepository;
    private readonly ImageFileRepository _imageRepository;
    private readonly TaskRepository _taskRepository;

    public ChannelImagesMigration(
        NpgsqlConnection connection,
        OwnerRepository ownerRepository,
        ChannelRepository channelRepository,
        ImageFileRepository imageRepository,
        TaskRepository taskRepository)
    {
        _connection = connection;
        _ownerRepository = ownerRepository;
        _channelRepository = channelRepository;
        _imageRepository = imageRepository;
        _taskRepository = taskRepository;
    }

    /// <inheritdoc />
    public async ValueTask MigrateAsync(CancellationToken cancellationToken)
    {
        var system = SystemPrincipals.StartupMigrations;
        List<ChannelEntity> channels;

        await using (var transaction = await _connection.OpenAndBeginTransaction(ReadCommitted, cancellationToken))
        {
            channels = await _channelRepository.GetForSystemAsync(system, transaction);

            await transaction.CommitAsync(cancellationToken);
        }

        foreach (var channel in channels)
        {
            await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);

            if (await _ownerRepository.GetPersonalUserIdForSystemAsync(system, channel.OwnerId, transaction) is not { } userId)
            {
                continue;
            }

            var images = await _imageRepository.GetForChannel(channel.Id, userId, Permissions.Read, transaction, cancellationToken);
            if (images is not [])
            {
                continue;
            }

            var libraryId = await _channelRepository.GetPrimaryLibraryId(channel.Id, userId, transaction, cancellationToken);
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
                if (await _taskRepository.TryAddTaskForSystemAsync(system, task, transaction) is { } taskId)
                {
                    await _taskRepository.TriggerTaskForSystemAsync(system, taskId, TaskSource.Schedule, transaction);
                }
            }

            await transaction.CommitAsync(cancellationToken);
        }
    }
}

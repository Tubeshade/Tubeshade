using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Identity;
using Tubeshade.Data.Media;
using Tubeshade.Data.Tasks;
using static System.Data.IsolationLevel;

namespace Tubeshade.Server.Configuration.Startup.Migrations;

public sealed class TrackFileMigration : IApplicationMigration
{
    private readonly NpgsqlConnection _connection;
    private readonly UserRepository _userRepository;
    private readonly LibraryRepository _libraryRepository;
    private readonly TaskRepository _taskRepository;

    public TrackFileMigration(
        NpgsqlConnection connection,
        UserRepository userRepository,
        LibraryRepository libraryRepository,
        TaskRepository taskRepository)
    {
        _connection = connection;
        _userRepository = userRepository;
        _libraryRepository = libraryRepository;
        _taskRepository = taskRepository;
    }

    /// <inheritdoc />
    public async ValueTask MigrateAsync(CancellationToken cancellationToken)
    {
        Guid userId;
        List<LibraryEntity> libraries;

        await using (var transaction = await _connection.OpenAndBeginTransaction(ReadCommitted, cancellationToken))
        {
            userId = await _userRepository.GetSystemUserId(transaction);
            libraries = await _libraryRepository.GetAsync(userId, transaction);

            await transaction.CommitAsync(cancellationToken);
        }

        foreach (var library in libraries)
        {
            await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);
            var tasks = await _taskRepository.GetRunningTasks(
                new TaskParameters
                {
                    UserId = userId,
                    LibraryId = library.Id,
                    Type = TaskType.RefreshTrackFiles,
                    Limit = 1,
                    Offset = 0,
                },
                cancellationToken);

            if (tasks is [])
            {
                var task = TaskEntity.RefreshTracks(library.Id, userId);
                if (await _taskRepository.TryAddTask(task, transaction) is { } taskId)
                {
                    await _taskRepository.TriggerTask(taskId, TaskSource.Schedule, transaction);
                }
            }

            await transaction.CommitAsync(cancellationToken);
        }
    }
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.AccessControl;
using Tubeshade.Data.Media;
using Tubeshade.Data.Tasks;
using static System.Data.IsolationLevel;

namespace Tubeshade.Server.Configuration.Startup.Migrations;

public sealed class FileMetadataMigration : IApplicationMigration
{
    private readonly NpgsqlConnection _connection;
    private readonly OwnerRepository _ownerRepository;
    private readonly LibraryRepository _libraryRepository;
    private readonly TaskRepository _taskRepository;

    public FileMetadataMigration(
        NpgsqlConnection connection,
        OwnerRepository ownerRepository,
        LibraryRepository libraryRepository,
        TaskRepository taskRepository)
    {
        _connection = connection;
        _ownerRepository = ownerRepository;
        _libraryRepository = libraryRepository;
        _taskRepository = taskRepository;
    }

    /// <inheritdoc />
    public async ValueTask MigrateAsync(CancellationToken cancellationToken)
    {
        var system = SystemPrincipals.StartupMigrations;
        List<LibraryEntity> libraries;

        await using (var transaction = await _connection.OpenAndBeginTransaction(ReadCommitted, cancellationToken))
        {
            libraries = await _libraryRepository.GetForSystemAsync(system, transaction);

            await transaction.CommitAsync(cancellationToken);
        }

        foreach (var library in libraries)
        {
            await using var transaction = await _connection.OpenAndBeginTransaction(cancellationToken);

            if (await _ownerRepository.GetPersonalUserIdForSystemAsync(system, library.OwnerId, transaction) is not { } userId)
            {
                continue;
            }

            var tasks = await _taskRepository.GetRunningTasks(
                new TaskParameters
                {
                    UserId = userId,
                    LibraryId = library.Id,
                    Type = TaskType.RefreshFileMetadata,
                    Limit = 1,
                    Offset = 0,
                },
                transaction,
                cancellationToken);

            if (tasks is [])
            {
                var task = TaskEntity.RefreshMetadata(library.Id, userId);
                if (await _taskRepository.TryAddTaskForSystemAsync(system, task, transaction) is { } taskId)
                {
                    await _taskRepository.TriggerTaskForSystemAsync(system, taskId, TaskSource.Schedule, transaction);
                }
            }

            await transaction.CommitAsync(cancellationToken);
        }
    }
}

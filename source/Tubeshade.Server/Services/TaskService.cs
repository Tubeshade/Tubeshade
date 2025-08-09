using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Tasks;
using Tubeshade.Data.Tasks.Payloads;

namespace Tubeshade.Server.Services;

public sealed class TaskService
{
    private readonly NpgsqlConnection _connection;
    private readonly TaskRepository _taskRepository;

    public TaskService(NpgsqlConnection connection, TaskRepository taskRepository)
    {
        _connection = connection;
        _taskRepository = taskRepository;
    }

    public async ValueTask ScanSubscriptions(Guid userId, IEnumerable<Guid> libraryIds)
    {
        await using var transaction = await _connection.OpenAndBeginTransaction();
        await ScanSubscriptions(userId, libraryIds, transaction);
        await transaction.CommitAsync();
    }

    public async ValueTask ScanSubscriptions(Guid userId, IEnumerable<Guid> libraryIds, NpgsqlTransaction transaction)
    {
        foreach (var id in libraryIds)
        {
            var payload = new ScanSubscriptionsPayload { LibraryId = id, UserId = userId };
            var taskId = await _taskRepository.AddScanSubscriptionsTask(payload, userId, transaction);
            await _taskRepository.TriggerTask(taskId, userId, transaction);
        }
    }

    public async ValueTask ScanSegments(Guid userId, IEnumerable<Guid> libraryIds)
    {
        await using var transaction = await _connection.OpenAndBeginTransaction();
        await ScanSegments(userId, libraryIds, transaction);
        await transaction.CommitAsync();
    }

    public async ValueTask ScanSegments(Guid userId, IEnumerable<Guid> libraryIds, NpgsqlTransaction transaction)
    {
        foreach (var id in libraryIds)
        {
            var payload = new ScanSponsorBlockSegmentsPayload { LibraryId = id, UserId = userId };
            var taskId = await _taskRepository.AddScanSegmentsTask(payload, userId, transaction);
            await _taskRepository.TriggerTask(taskId, userId, transaction);
        }
    }

    public async ValueTask RetryTask(Guid userId, Guid taskId)
    {
        await using var transaction = await _connection.OpenAndBeginTransaction();
        await _taskRepository.TriggerTask(taskId, userId, transaction);
        await transaction.CommitAsync();
    }

    public async ValueTask CancelTaskRun(Guid userId, Guid taskRunId)
    {
        await using var transaction = await _connection.OpenAndBeginTransaction();
        await _taskRepository.CancelTaskRun(taskRunId, userId, transaction);
        await transaction.CommitAsync();
    }
}

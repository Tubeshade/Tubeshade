using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Tasks;
using Tubeshade.Data.Tasks.Payloads;
using Tubeshade.Server.Pages.Libraries.Tasks;

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

    public async ValueTask<List<TaskModel>> GetGroupedTasks(
        TaskParameters parameters,
        CancellationToken cancellationToken)
    {
        var runningTasks = await _taskRepository.GetRunningTasks(parameters, cancellationToken);
        return GroupTasks(runningTasks);
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

    public async ValueTask IndexVideo(Guid userId, Guid libraryId, string url)
    {
        await using var transaction = await _connection.OpenAndBeginTransaction();
        await IndexVideo(userId, libraryId, url, transaction);
        await transaction.CommitAsync();
    }

    public async ValueTask IndexVideo(Guid userId, Guid libraryId, string url, NpgsqlTransaction transaction)
    {
        var payload = new IndexPayload { Url = url, UserId = userId, LibraryId = libraryId };
        var taskId = await _taskRepository.AddIndexTask(payload, userId, transaction);
        await _taskRepository.TriggerTask(taskId, userId, transaction);
    }

    public async ValueTask DownloadVideo(Guid userId, Guid libraryId, Guid videoId)
    {
        await using var transaction = await _connection.OpenAndBeginTransaction();
        await DownloadVideo(userId, libraryId, videoId, transaction);
        await transaction.CommitAsync();
    }

    public async ValueTask DownloadVideo(Guid userId, Guid libraryId, Guid videoId, NpgsqlTransaction transaction)
    {
        var payload = new DownloadVideoPayload { LibraryId = libraryId, VideoId = videoId, UserId = userId };
        var taskId = await _taskRepository.AddDownloadTask(payload, userId, transaction);
        await _taskRepository.TriggerTask(taskId, userId, transaction);
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

    private static List<TaskModel> GroupTasks(IEnumerable<RunningTaskEntity> runningTasks)
    {
        return runningTasks
            .GroupBy(task => task.Id)
            .Select(grouping =>
            {
                var runs = grouping.ToArray();

                return new TaskModel
                {
                    Id = grouping.Key,
                    Type = runs[0].Type,
                    Name = runs[0].Name,
                    TotalCount = runs[0].TotalCount,
                    Runs = runs
                        .Select(task => new TaskRunModel
                        {
                            Id = task.RunId,
                            Value = task.Value,
                            Target = task.Target,
                            Rate = task.Rate,
                            Remaining = task.RemainingDuration,
                            Result = task.Result,
                            Message = task.Message,
                        })
                        .ToArray(),
                };
            })
            .ToList();
    }
}

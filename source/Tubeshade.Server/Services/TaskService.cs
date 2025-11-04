using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Pages.Tasks;

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
            var taskId = await _taskRepository.AddScanSubscriptionsTask(id, userId, transaction);
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
            var taskId = await _taskRepository.AddScanSegmentsTask(id, userId, transaction);
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
        var taskId = await _taskRepository.AddIndexTask(url, libraryId, userId, transaction);
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
        var taskId = await _taskRepository.AddDownloadTask(videoId, libraryId, userId, transaction);
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
                var task = runs[0];

                return new TaskModel
                {
                    Id = grouping.Key,
                    Type = task.Type,
                    Name = task.Name,
                    LibraryId = task.LibraryId,
                    ChannelId = task.ChannelId,
                    VideoId = task.VideoId,
                    TotalCount = task.TotalCount,
                    Runs = runs
                        .Select(run => new TaskRunModel
                        {
                            Id = run.RunId,
                            Value = run.Value,
                            Target = run.Target,
                            Rate = run.Rate,
                            Remaining = run.RemainingDuration,
                            Result = run.Result,
                            Message = run.Message,
                        })
                        .ToArray(),
                };
            })
            .ToList();
    }
}

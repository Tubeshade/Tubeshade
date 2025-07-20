using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Tasks;

namespace Tubeshade.Server.Services.Background;

public sealed class TaskListenerBackgroundService : BackgroundService
{
    private static readonly ConcurrentDictionary<TaskType, bool> ChannelStatus = [];

    private static readonly FrozenDictionary<TaskType, Channel<Guid>> Channels = TaskType.List
        .ToFrozenDictionary(
            type => type,
            _ => Channel.CreateUnbounded<Guid>(
                new UnboundedChannelOptions
                {
                    AllowSynchronousContinuations = true,
                    SingleReader = true,
                    SingleWriter = true,
                }));

    private readonly ILogger<TaskListenerBackgroundService> _logger;
    private readonly NpgsqlMultiHostDataSource _dataSource;

    public TaskListenerBackgroundService(
        ILogger<TaskListenerBackgroundService> logger,
        NpgsqlMultiHostDataSource dataSource)
    {
        _logger = logger;
        _dataSource = dataSource;
    }

    internal static ChannelReader<Guid> GetChannelForTasks(TaskType taskType)
    {
        if (ChannelStatus.TryAdd(taskType, true))
        {
            return Channels[taskType].Reader;
        }

        throw new InvalidOperationException($"Trying to access channel reader for {taskType.Name} multiple times");
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var notificationConnection = _dataSource.CreateConnection(TargetSessionAttributes.Any);
        await notificationConnection.OpenConnection(stoppingToken);

        _logger.LogInformation("Starting to listen for created tasks");
        notificationConnection.Notification += (_, args) =>
        {
            using var connection = _dataSource.CreateConnection(TargetSessionAttributes.Any);
            ConnectionOnNotification(connection, args);
        };

        await notificationConnection.ExecuteAsync("LISTEN task_created;");
        while (!stoppingToken.IsCancellationRequested)
        {
            await notificationConnection.WaitAsync(stoppingToken);
        }
    }

    private void ConnectionOnNotification(NpgsqlConnection connection, NpgsqlNotificationEventArgs args)
    {
        _logger.LogDebug("Received notification {NotificationChannel} from {NotificationPid}", args.Channel, args.PID);
        if (args.Channel is not "task_created")
        {
            return;
        }

        _logger.LogDebug("Received created task notification with payload {NotificationPayload}", args.Payload);
        if (!Guid.TryParse(args.Payload, out var taskId))
        {
            _logger.LogWarning("Failed to parse task id from notification payload {NotificationPayload}", args.Payload);
            return;
        }

        var taskType = connection.QuerySingle<TaskType>(
            "SELECT type FROM tasks.tasks WHERE tasks.id = @taskId;",
            new { taskId });

        _logger.LogInformation("Starting task {TaskType} {TaskId}", taskType.Name, taskId);

        var queued = Channels[taskType].Writer.TryWrite(taskId);
        Trace.Assert(queued);
    }
}

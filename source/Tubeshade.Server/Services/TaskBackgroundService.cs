using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Tasks;

namespace Tubeshade.Server.Services;

public sealed class TaskBackgroundService : BackgroundService
{
    public static readonly Channel<Guid> IndexTaskChannel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = true,
            SingleReader = true,
            SingleWriter = true,
        });

    public static readonly Channel<Guid> DownloadTaskChannel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = true,
            SingleReader = true,
            SingleWriter = true,
        });

    private readonly ILogger<TaskBackgroundService> _logger;
    private readonly NpgsqlMultiHostDataSource _dataSource;

    public TaskBackgroundService(NpgsqlMultiHostDataSource dataSource, ILogger<TaskBackgroundService> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var connection = _dataSource.CreateConnection(TargetSessionAttributes.Any);
        await connection.OpenConnection(stoppingToken);

        _logger.LogInformation("Starting to listen for created tasks");
        connection.Notification += (_, args) =>
        {
            using var c = _dataSource.CreateConnection(TargetSessionAttributes.Any);
            ConnectionOnNotification(c, args);
        };
        await connection.ExecuteAsync("LISTEN task_created;");

        while (!stoppingToken.IsCancellationRequested)
        {
            await connection.WaitAsync(stoppingToken);
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
            return;
        }

        var foo = connection.QuerySingle<TaskType>(
            "SELECT type FROM tasks.tasks WHERE tasks.id = @taskId;",
            new { taskId });

        _logger.LogInformation("Starting task {TaskId}", taskId);

        _ = foo.Name switch
        {
            "index_video" => IndexTaskChannel.Writer.TryWrite(taskId),
            "download_video" => DownloadTaskChannel.Writer.TryWrite(taskId),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }
}

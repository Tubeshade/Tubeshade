using System;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Configuration.Startup;

namespace Tubeshade.Server.Services.Background;

public sealed class TaskListenerService : BackgroundService
{
    private readonly Channel<CreatedTask> _taskCreatedChannel = CreateUnboundedChannel<CreatedTask>();

    private readonly FrozenDictionary<string, Channel<Guid>> _channels = TaskChannels.Names
        .Where(name => name is not TaskChannels.Created)
        .ToFrozenDictionary(name => name, _ => CreateUnboundedChannel<Guid>());

    private readonly ILogger<TaskListenerService> _logger;
    private readonly NpgsqlMultiHostDataSource _dataSource;
    private readonly DatabaseMigrationStartupFilter _migrationStartupFilter;

    internal ChannelReader<CreatedTask> TaskCreated => _taskCreatedChannel.Reader;

    internal ChannelReader<Guid> TaskRunCancelled => _channels[TaskChannels.Cancel].Reader;

    internal event EventHandler<Guid>? TaskRunFinished;

    public TaskListenerService(
        ILogger<TaskListenerService> logger,
        NpgsqlMultiHostDataSource dataSource,
        DatabaseMigrationStartupFilter migrationStartupFilter)
    {
        _logger = logger;
        _dataSource = dataSource;
        _migrationStartupFilter = migrationStartupFilter;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _migrationStartupFilter.MigrationTask;

        await using var connection = _dataSource.CreateConnection(TargetSessionAttributes.Any);
        await connection.OpenConnection(stoppingToken);

        _logger.ListeningToDatabaseNotifications();
        connection.Notification += ConnectionOnNotification;

        foreach (var channel in TaskChannels.Names)
        {
            _logger.ListeningToNotificationChannel(channel);
            await connection.ExecuteAsync($"LISTEN {channel};");
        }

        _logger.ListeningToDatabaseNotifications(connection.DataSource);
        while (!stoppingToken.IsCancellationRequested)
        {
            await connection.WaitAsync(stoppingToken);
        }

        connection.Notification -= ConnectionOnNotification;
    }

    private void ConnectionOnNotification(object? sender, NpgsqlNotificationEventArgs args)
    {
        _logger.ReceivedNotification(args.Channel, args.PID);

        if (args.Channel is TaskChannels.Created)
        {
            _logger.ReceivedNotification(args.Channel, args.Payload);
            var parts = args.Payload.Split('|', StringSplitOptions.RemoveEmptyEntries);
            if (!Guid.TryParse(parts[0], out var taskId))
            {
                _logger.UnexpectedNotificationPayload(args.Payload);
                return;
            }

            TaskSource? source = null;
            if (parts.Length is 1)
            {
                source = TaskSource.Unknown;
            }
            else if (parts is [_, var sourceName])
            {
                _ = TaskSource.TryFromName(sourceName, out source);
            }

            if (source is null)
            {
                _logger.UnexpectedNotificationPayload(args.Payload);
                return;
            }

            _logger.QueueingNotification(args.Channel, taskId, source.Name);
            var queued = _taskCreatedChannel.Writer.TryWrite(new CreatedTask { Id = taskId, Source = source });
            Trace.Assert(queued);
        }
        else
        {
            if (!_channels.TryGetValue(args.Channel, out var channel))
            {
                _logger.UnexpectedNotificationChannel(args.Channel);
                return;
            }

            _logger.ReceivedNotification(args.Channel, args.Payload);
            if (!Guid.TryParse(args.Payload, out var id))
            {
                _logger.UnexpectedNotificationPayload(args.Payload);
                return;
            }

            _logger.QueueingNotification(args.Channel, id);
            var queued = channel.Writer.TryWrite(id);
            Trace.Assert(queued);

            if (args.Channel is TaskChannels.RunFinished)
            {
                TaskRunFinished?.Invoke(this, id);
            }
        }
    }

    private static Channel<TData> CreateUnboundedChannel<TData>() => Channel.CreateUnbounded<TData>(
        new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = true,
            SingleReader = true,
            SingleWriter = true,
        });
}

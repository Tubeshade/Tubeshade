using System;
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

public sealed class TaskListenerService : BackgroundService
{
    private static readonly FrozenDictionary<string, Channel<Guid>> Channels = TaskChannels.Names
        .ToFrozenDictionary(name => name, _ => Channel.CreateUnbounded<Guid>(
            new UnboundedChannelOptions
            {
                AllowSynchronousContinuations = true,
                SingleReader = true,
                SingleWriter = true,
            }));

    private readonly ILogger<TaskListenerService> _logger;
    private readonly NpgsqlMultiHostDataSource _dataSource;

    internal static ChannelReader<Guid> TaskCreated => Channels[TaskChannels.Created].Reader;

    internal static ChannelReader<Guid> TaskRunCancelled => Channels[TaskChannels.Cancel].Reader;

    public TaskListenerService(
        ILogger<TaskListenerService> logger,
        NpgsqlMultiHostDataSource dataSource)
    {
        _logger = logger;
        _dataSource = dataSource;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var connection = _dataSource.CreateConnection(TargetSessionAttributes.Any);
        await connection.OpenConnection(stoppingToken);

        _logger.LogDebug("Starting to listen database notifications");
        connection.Notification += ConnectionOnNotification;

        foreach (var channel in TaskChannels.Names)
        {
            _logger.LogDebug("Starting to listen on channel {NotificationChannel}", channel);
            await connection.ExecuteAsync($"LISTEN {channel};");
        }

        _logger.LogInformation("Listening for database notifications from {Datasource}", connection.DataSource);
        while (!stoppingToken.IsCancellationRequested)
        {
            await connection.WaitAsync(stoppingToken);
        }

        connection.Notification -= ConnectionOnNotification;
    }

    private void ConnectionOnNotification(object? sender, NpgsqlNotificationEventArgs args)
    {
        _logger.LogDebug("Received notification {NotificationChannel} from {NotificationPid}", args.Channel, args.PID);
        if (!Channels.TryGetValue(args.Channel, out var channel))
        {
            _logger.LogWarning("Unexpected notification channel {NotificationChannel}", args.Channel);
            return;
        }

        _logger.LogDebug("Received created task notification with payload {NotificationPayload}", args.Payload);
        if (!Guid.TryParse(args.Payload, out var id))
        {
            _logger.LogWarning("Failed to parse task id from notification payload {NotificationPayload}", args.Payload);
            return;
        }

        _logger.LogInformation("Starting task {TaskId}", id);
        var queued = channel.Writer.TryWrite(id);
        Trace.Assert(queued);
    }
}

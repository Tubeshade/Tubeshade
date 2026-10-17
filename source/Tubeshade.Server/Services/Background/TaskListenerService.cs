using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Configuration.Startup;

namespace Tubeshade.Server.Services.Background;

public sealed class TaskListenerService : BackgroundService
{
    private readonly TaskCompletionSource _taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly FrozenDictionary<string, Channel<Guid>> _channels = TaskChannels.Names
        .ToFrozenDictionary(
            name => name,
            name => CreateUnboundedChannel<Guid>(singleWriter: name is not TaskChannels.Requested));

    private readonly ILogger<TaskListenerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly NpgsqlMultiHostDataSource _dataSource;
    private readonly DatabaseMigrationStartupFilter _migrationStartupFilter;

    private int _connectionAttempt;

    internal ChannelReader<Guid> TaskRequested => _channels[TaskChannels.Requested].Reader;

    internal ChannelReader<Guid> TaskRunCancelled => _channels[TaskChannels.Cancel].Reader;

    internal event EventHandler<Guid>? TaskRunFinished;

    internal event EventHandler? ListeningResumed;

    internal Task IsListeningTask => _taskCompletionSource.Task;

    public TaskListenerService(
        ILogger<TaskListenerService> logger,
        IServiceProvider serviceProvider,
        NpgsqlMultiHostDataSource dataSource,
        DatabaseMigrationStartupFilter migrationStartupFilter)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _dataSource = dataSource;
        _migrationStartupFilter = migrationStartupFilter;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _migrationStartupFilter.MigrationTask.WaitAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ListenForNotifications(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                var delay = RetryDelay.GetExponential(_connectionAttempt);
                _connectionAttempt++;
                _logger.ListeningToDatabaseNotificationsFailed(exception, delay);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }

    private async ValueTask ListenForNotifications(CancellationToken stoppingToken)
    {
        await using var connection = _dataSource.CreateConnection(TargetSessionAttributes.Any);
        await connection.OpenConnection(stoppingToken);

        _logger.ListeningToDatabaseNotifications();
        connection.Notification += ConnectionOnNotification;

        try
        {
            foreach (var channel in TaskChannels.Names)
            {
                _logger.ListeningToNotificationChannel(channel);
                await connection.ExecuteAsync($"LISTEN {channel};");
            }

            _logger.ListeningToDatabaseNotifications(connection.DataSource);
            ListeningResumed?.Invoke(this, EventArgs.Empty);

            await QueuePendingTaskRequests(stoppingToken);

            _ = _taskCompletionSource.TrySetResult();
            _connectionAttempt = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                await connection.WaitAsync(stoppingToken);
            }
        }
        finally
        {
            connection.Notification -= ConnectionOnNotification;
        }
    }

    private async ValueTask QueuePendingTaskRequests(CancellationToken cancellationToken)
    {
        var requestIds = await GetTaskRequestIds(cancellationToken);
        _logger.QueueingPendingTaskRequests(requestIds.Count);

        var writer = _channels[TaskChannels.Requested].Writer;
        foreach (var requestId in requestIds)
        {
            await writer.WriteAsync(requestId, cancellationToken);
        }
    }

    private async ValueTask<List<Guid>> GetTaskRequestIds(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<TaskRepository>();
        return await repository.GetTaskRequestIds(cancellationToken);
    }

    private void ConnectionOnNotification(object? sender, NpgsqlNotificationEventArgs args)
    {
        _logger.ReceivedNotification(args.Channel, args.PID);

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

        if (args.Channel is TaskChannels.RunFinished)
        {
            TaskRunFinished?.Invoke(this, id);
        }
        else
        {
            _logger.QueueingNotification(args.Channel, id);
            var queued = channel.Writer.TryWrite(id);
            Trace.Assert(queued);
        }
    }

    private static Channel<TData> CreateUnboundedChannel<TData>(bool singleWriter) => Channel.CreateUnbounded<TData>(
        new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = true,
            SingleReader = true,
            SingleWriter = singleWriter,
        });
}

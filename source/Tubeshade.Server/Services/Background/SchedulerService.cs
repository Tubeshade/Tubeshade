using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Text;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Configuration;
using static System.Data.IsolationLevel;

namespace Tubeshade.Server.Services.Background;

public sealed class SchedulerService : BackgroundService
{
    private static readonly Func<ILogger, Instant, Instant, Duration, IDisposable?> SchedulerTickScope = LoggerMessage
        .DefineScope<Instant, Instant, Duration>("{IntervalStart} - {IntervalEnd} ({IntervalDuration})");

    private readonly ILogger<SchedulerService> _logger;
    private readonly IClock _clock;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<SchedulerOptions> _optionsMonitor;
    private readonly TaskListenerService _taskListenerService;

    public SchedulerService(
        ILogger<SchedulerService> logger,
        IClock clock,
        IServiceProvider serviceProvider,
        IOptionsMonitor<SchedulerOptions> optionsMonitor,
        TaskListenerService taskListenerService)
    {
        _logger = logger;
        _clock = clock;
        _serviceProvider = serviceProvider;
        _optionsMonitor = optionsMonitor;
        _taskListenerService = taskListenerService;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _taskListenerService.IsListeningTask.WaitAsync(stoppingToken);

        var timerPeriod = _optionsMonitor.CurrentValue.GetPeriodTimeSpan();
        using var timer = new PeriodicTimer(timerPeriod);
        // timer has the same scope as change monitor
        // ReSharper disable once AccessToDisposedClosure
        using var changeMonitor = _optionsMonitor.OnChange(options => OnOptionsChange(options, timer));

        var beforeTick = _clock.GetCurrentInstant();

        // Schedules triggered since beforeTick, so that they are not triggered again
        // when the next tick covers a failed interval as well.
        var triggeredScheduleIds = new HashSet<Guid>();

        _logger.StartingScheduler(timer.Period);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var afterTick = _clock.GetCurrentInstant();
            var interval = new Interval(beforeTick, afterTick);

            using (SchedulerTickScope(_logger, interval.Start, interval.End, interval.Duration))
            {
                _logger.SchedulerTick();
            }

            try
            {
                await ProcessScheduleTick(beforeTick, interval, triggeredScheduleIds, stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                // beforeTick is not updated, so that the next tick covers the failed interval as well
                _logger.SchedulerTickFailed(exception);
                continue;
            }

            beforeTick = afterTick;
            triggeredScheduleIds.Clear();
        }
    }

    private async Task ProcessScheduleTick(
        Instant beforeTick,
        Interval interval,
        HashSet<Guid> triggeredScheduleIds,
        CancellationToken stoppingToken)
    {
        await using var tickScope = _serviceProvider.CreateAsyncScope();

        var scheduleRepository = tickScope.ServiceProvider.GetRequiredService<ScheduleRepository>();
        var schedules = await scheduleRepository.GetWithoutAccessControl(stoppingToken);

        var taskRepository = tickScope.ServiceProvider.GetRequiredService<TaskRepository>();
        var connection = tickScope.ServiceProvider.GetRequiredService<NpgsqlConnection>();

        foreach (var schedule in schedules)
        {
            using var scheduleScope = _logger.BeginScope("{ScheduleId}", schedule.Id);
            var nextTime = GetNextTime(schedule.CronExpression, beforeTick, schedule.Id.GetHashCode());
            if (!interval.Contains(nextTime) || triggeredScheduleIds.Contains(schedule.Id))
            {
                _logger.SkippingSchedule();
                continue;
            }

            _logger.StartingScheduledTask(schedule.TaskId);

            await using var transaction = await connection.OpenAndBeginTransaction(ReadCommitted, stoppingToken);
            await taskRepository.TriggerTask(schedule.TaskId, TaskSource.Schedule, transaction);
            await transaction.CommitAsync(stoppingToken);
            triggeredScheduleIds.Add(schedule.Id);
        }
    }

    public static Instant GetNextTime(string cron, Instant currentTime, int seed)
    {
        var expression = CronExpression.Parse(cron, seed);
        if (expression.GetNextOccurrence(currentTime.ToDateTimeUtc()) is { } nextTime)
        {
            return Instant.FromDateTimeUtc(nextTime);
        }

        var time = InstantPattern.ExtendedIso.Format(currentTime);
        throw new InvalidOperationException($"Failed to find next occurrence for expression {cron} from {time}");
    }

    private void OnOptionsChange(SchedulerOptions options, PeriodicTimer timer)
    {
        var newPeriod = options.GetPeriodTimeSpan();
        if (timer.Period == newPeriod)
        {
            _logger.SchedulerPeriodUnchanged();
            return;
        }

        _logger.SchedulerPeriodChanged(timer.Period, newPeriod);
        timer.Period = newPeriod;
    }
}

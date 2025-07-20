using System;
using System.Threading;
using System.Threading.Tasks;
using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using Npgsql;
using Tubeshade.Data;
using Tubeshade.Data.Tasks;
using Tubeshade.Server.Configuration;

namespace Tubeshade.Server.Services.Background;

public sealed class SchedulerBackgroundService : BackgroundService
{
    private static readonly Func<ILogger, Instant, Instant, Duration, IDisposable?> SchedulerTickScope = LoggerMessage
        .DefineScope<Instant, Instant, Duration>("{IntervalStart} - {IntervalEnd} ({IntervalDuration})");

    private readonly ILogger<SchedulerBackgroundService> _logger;
    private readonly IClock _clock;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<SchedulerOptions> _optionsMonitor;

    public SchedulerBackgroundService(
        ILogger<SchedulerBackgroundService> logger,
        IClock clock,
        IServiceProvider serviceProvider,
        IOptionsMonitor<SchedulerOptions> optionsMonitor)
    {
        _logger = logger;
        _clock = clock;
        _serviceProvider = serviceProvider;
        _optionsMonitor = optionsMonitor;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timerPeriod = _optionsMonitor.CurrentValue.GetPeriodTimeSpan();
        using var timer = new PeriodicTimer(timerPeriod);
        // timer has the same scope as change monitor
        // ReSharper disable once AccessToDisposedClosure
        using var changeMonitor = _optionsMonitor.OnChange(options => OnOptionsChange(options, timer));

        var beforeTick = _clock.GetCurrentInstant();

        _logger.LogDebug("Starting scheduler with period {SchedulerPeriod}", timer.Period);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var afterTick = _clock.GetCurrentInstant();
            var interval = new Interval(beforeTick, afterTick);

            using (SchedulerTickScope(_logger, interval.Start, interval.End, interval.Duration))
            {
                _logger.LogDebug("Scheduler tick");
            }

            await using var tickScope = _serviceProvider.CreateAsyncScope();

            var scheduleRepository = tickScope.ServiceProvider.GetRequiredService<ScheduleRepository>();
            var schedules = await scheduleRepository.GetAsync(Guid.Empty, stoppingToken);

            var taskRepository = tickScope.ServiceProvider.GetRequiredService<TaskRepository>();
            var connection = tickScope.ServiceProvider.GetRequiredService<NpgsqlConnection>();

            foreach (var schedule in schedules)
            {
                using var scheduleScope = _logger.BeginScope("{ScheduleId}", schedule.Id);
                var nextTime = GetNextTime(schedule.CronExpression, beforeTick);
                if (!interval.Contains(nextTime))
                {
                    _logger.LogTrace("Skipping schedule");
                    continue;
                }

                _logger.LogDebug("Starting task {TaskId} based on schedule", schedule.TaskId);

                await using var transaction = await connection.OpenAndBeginTransaction(stoppingToken);
                await taskRepository.TriggerTask(schedule.TaskId, transaction);
                await transaction.CommitAsync(stoppingToken);
            }

            beforeTick = afterTick;
        }
    }

    public static Instant GetNextTime(string cron, Instant currentTime)
    {
        var expression = CronExpression.Parse(cron);
        var next = expression.GetNextOccurrence(currentTime.ToDateTimeUtc());
        return Instant.FromDateTimeUtc(next!.Value);
    }

    private void OnOptionsChange(SchedulerOptions options, PeriodicTimer timer)
    {
        var newPeriod = options.GetPeriodTimeSpan();
        if (timer.Period == newPeriod)
        {
            _logger.LogDebug("Scheduler period has not changed");
            return;
        }

        _logger.LogDebug("Updating scheduler period from {OldSchedulerPeriod} to {NewSchedulerPeriod}", timer.Period, newPeriod);
        timer.Period = newPeriod;
    }
}

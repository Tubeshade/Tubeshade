using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tubeshade.Server.Configuration.Startup.Migrations;
using Tubeshade.Server.Services;
using Tubeshade.Server.Services.Background;

namespace Tubeshade.Server.Configuration.Startup;

public sealed class ApplicationMigrationService : BackgroundService
{
    private readonly ILogger<ApplicationMigrationService> _logger;
    private readonly TaskListenerService _taskListenerService;
    private readonly IServiceProvider _serviceProvider;

    public ApplicationMigrationService(
        ILogger<ApplicationMigrationService> logger,
        TaskListenerService taskListenerService,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _taskListenerService = taskListenerService;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _taskListenerService.IsListeningTask.WaitAsync(stoppingToken);

        await ApplyMigration<FileMetadataMigration>(nameof(FileMetadataMigration), stoppingToken);
        await ApplyMigration<TrackFileMigration>(nameof(TrackFileMigration), stoppingToken);

        _logger.ApplicationMigrationsApplied();
    }

    private async ValueTask ApplyMigration<TMigration>(string name, CancellationToken cancellationToken)
        where TMigration : IApplicationMigration
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        _logger.ApplyingApplicationMigration(name);
        var migration = scope.ServiceProvider.GetRequiredService<TMigration>();
        await migration.MigrateAsync(cancellationToken);
    }
}

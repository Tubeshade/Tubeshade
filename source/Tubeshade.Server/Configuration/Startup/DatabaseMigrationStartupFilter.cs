using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Tubeshade.Data.Migrations;
using Tubeshade.Data.Tasks;

namespace Tubeshade.Server.Configuration.Startup;

public sealed class DatabaseMigrationStartupFilter : IStartupFilter
{
    private readonly TaskCompletionSource _taskCompletionSource = new();

    internal Task MigrationTask => _taskCompletionSource.Task;

    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => builder =>
    {
        using (var scope = builder.ApplicationServices.CreateScope())
        {
            var migrationService = scope.ServiceProvider.GetRequiredService<DatabaseMigrationService>();
            migrationService.Migrate();
        }

        using (var scope = builder.ApplicationServices.CreateScope())
        {
            var dataSource = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
            dataSource.ReloadTypes();
            dataSource.Clear();
        }

        using (var scope = builder.ApplicationServices.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<TaskRepository>();
            repository.CompleteStuckTasks(CancellationToken.None).AsTask().GetAwaiter().GetResult();
        }

        _taskCompletionSource.SetResult();

        next(builder);
    };
}

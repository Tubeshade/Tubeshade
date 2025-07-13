using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Tubeshade.Data.Migrations;

namespace Tubeshade.Server.Configuration.Startup;

internal sealed class DatabaseMigrationStartupFilter : IStartupFilter
{
    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => builder =>
    {
        using (var scope = builder.ApplicationServices.CreateScope())
        {
            var migrationService = scope.ServiceProvider.GetRequiredService<DatabaseMigrationService>();
            migrationService.Migrate();
        }

        next(builder);
    };
}

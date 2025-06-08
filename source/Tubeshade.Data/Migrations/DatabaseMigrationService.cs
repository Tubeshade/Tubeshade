using System.Reflection;
using DbUp;
using Microsoft.Extensions.Logging;

namespace Tubeshade.Data.Migrations;

public sealed class DatabaseMigrationService
{
    private readonly ILoggerFactory _loggerFactory;

    public DatabaseMigrationService(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public void Migrate()
    {
        var upgradeEngine = DeployChanges.To
            .PostgresqlDatabase("todo")
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .LogTo(_loggerFactory)
            .Build();

        var result = upgradeEngine.PerformUpgrade();
        if (result.Successful)
        {
            return;
        }

        throw result.Error;
    }
}

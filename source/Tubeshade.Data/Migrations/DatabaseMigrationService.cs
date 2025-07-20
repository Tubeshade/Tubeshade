using System.Reflection;
using DbUp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tubeshade.Data.Configuration;

namespace Tubeshade.Data.Migrations;

public sealed class DatabaseMigrationService
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly DatabaseOptions _options;

    public DatabaseMigrationService(ILoggerFactory loggerFactory, IOptionsSnapshot<DatabaseOptions> options)
    {
        _loggerFactory = loggerFactory;
        _options = options.Value;
    }

    public void Migrate()
    {
        var upgradeEngine = DeployChanges
            .To.PostgresqlDatabase(_options.ConnectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .WithTransactionPerScript()
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

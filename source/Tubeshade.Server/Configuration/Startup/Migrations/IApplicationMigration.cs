using System.Threading;
using System.Threading.Tasks;

namespace Tubeshade.Server.Configuration.Startup.Migrations;

public interface IApplicationMigration
{
    ValueTask MigrateAsync(CancellationToken cancellationToken);
}

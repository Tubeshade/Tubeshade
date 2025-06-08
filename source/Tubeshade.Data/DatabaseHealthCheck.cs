using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Tubeshade.Data;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly NpgsqlDataSource _dataSource;

    public DatabaseHealthCheck(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = _dataSource.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            var command = new CommandDefinition("SELECT 1;", cancellationToken: cancellationToken);
            var result = await connection.QuerySingleAsync<int>(command);

            return result is 1
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Degraded("Test query does not return expected result");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Failed to query the database", exception);
        }
    }
}

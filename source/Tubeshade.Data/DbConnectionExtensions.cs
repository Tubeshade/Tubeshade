using System.Data;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Tubeshade.Data;

public static class DbConnectionExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task OpenConnection(
        this NpgsqlConnection connection,
        CancellationToken cancellationToken = default)
    {
        return (connection.State & ConnectionState.Open) is not ConnectionState.Open
            ? connection.OpenAsync(cancellationToken)
            : Task.CompletedTask;
    }

    public static ValueTask<NpgsqlTransaction> OpenAndBeginTransaction(
        this NpgsqlConnection connection,
        CancellationToken cancellationToken = default)
    {
        return OpenAndBeginTransaction(connection, IsolationLevel.Serializable, cancellationToken);
    }

    public static async ValueTask<NpgsqlTransaction> OpenAndBeginTransaction(
        this NpgsqlConnection connection,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default)
    {
        await connection.OpenConnection(cancellationToken);
        return await connection.BeginTransactionAsync(isolationLevel, cancellationToken);
    }

    public static async ValueTask CommitWithRetries(
        this NpgsqlTransaction transaction,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        while (true)
        {
            try
            {
                await transaction.CommitAsync(cancellationToken);
                return;
            }
            catch (PostgresException exception) when (exception.IsTransient)
            {
                logger.LogWarning(exception, "Failed to commit transaction, retrying");
                await Task.Delay(100, cancellationToken);
            }
        }
    }
}

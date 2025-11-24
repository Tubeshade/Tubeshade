using System;
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
    public static Task OpenConnection(this NpgsqlConnection connection, CancellationToken cancellationToken = default)
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

    public static async ValueTask ExecuteWithinTransaction(
        this NpgsqlConnection connection,
        ILogger logger,
        Func<NpgsqlTransaction, ValueTask> action,
        CancellationToken cancellationToken = default)
    {
        const int attempts = 5;

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            try
            {
                await using var transaction = await connection.OpenAndBeginTransaction(cancellationToken);
                await action(transaction);
                await transaction.CommitAsync(cancellationToken);
                return;
            }
            catch (PostgresException exception) when (exception.IsTransient && attempt < attempts - 1)
            {
                logger.LogWarning("Failed to commit transaction, retrying");
                await Task.Delay(50 * (int)Math.Pow(2, attempt), cancellationToken);
            }
        }
    }
}

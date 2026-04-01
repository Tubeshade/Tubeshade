using System;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Tubeshade.Data;

public static class DbConnectionExtensions
{
    extension(NpgsqlConnection connection)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task OpenConnection(CancellationToken cancellationToken = default)
        {
            return (connection.State & ConnectionState.Open) is not ConnectionState.Open
                ? connection.OpenAsync(cancellationToken)
                : Task.CompletedTask;
        }

        public ValueTask<NpgsqlTransaction> OpenAndBeginTransaction(CancellationToken cancellationToken = default)
        {
            return connection.OpenAndBeginTransaction(IsolationLevel.Serializable, cancellationToken);
        }

        public async ValueTask<NpgsqlTransaction> OpenAndBeginTransaction(IsolationLevel isolationLevel,
            CancellationToken cancellationToken = default)
        {
            await connection.OpenConnection(cancellationToken);
            return await connection.BeginTransactionAsync(isolationLevel, cancellationToken);
        }

        public async ValueTask<TValue> ExecuteWithinTransaction<TValue>(ILogger logger,
            Func<NpgsqlTransaction, ValueTask<TValue>> action,
            CancellationToken cancellationToken = default)
        {
            const int attempts = 5;

            for (var attempt = 0; attempt < attempts; attempt++)
            {
                try
                {
                    await using var transaction = await connection.OpenAndBeginTransaction(cancellationToken);
                    var value = await action(transaction);
                    await transaction.CommitAsync(cancellationToken);
                    return value;
                }
                catch (PostgresException exception) when (exception.IsTransient && attempt < attempts - 1)
                {
                    logger.LogWarning("Failed to commit transaction, retrying");
                    await Task.Delay(50 * (int)Math.Pow(2, attempt), cancellationToken);
                }
            }

            throw new UnreachableException();
        }

        public async ValueTask ExecuteWithinTransaction(ILogger logger,
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
}

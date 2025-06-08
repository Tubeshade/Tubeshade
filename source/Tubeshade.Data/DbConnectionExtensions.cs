using System.Data;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace Tubeshade.Data;

public static class DbConnectionExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task OpenConnection(
        this NpgsqlConnection dbConnection,
        CancellationToken cancellationToken = default)
    {
        return (dbConnection.State & ConnectionState.Open) is not ConnectionState.Open
            ? dbConnection.OpenAsync(cancellationToken)
            : Task.CompletedTask;
    }

    public static async ValueTask<NpgsqlTransaction> OpenAndBeginTransaction(
        this NpgsqlConnection dbConnection,
        CancellationToken cancellationToken = default)
    {
        await dbConnection.OpenConnection(cancellationToken);
        return await dbConnection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
    }
}

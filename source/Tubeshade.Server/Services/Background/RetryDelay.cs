using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace Tubeshade.Server.Services.Background;

internal static class RetryDelay
{
    internal static TimeSpan GetExponential(int attempt)
    {
        return TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, attempt)));
    }

    /// <summary>Whether the exception is caused by the database being unavailable, and might succeed if retried.</summary>
    /// <remarks>
    /// Errors returned by the server, such as constraint violations, are only transient if the server says so,
    /// while other database errors, such as broken connections or no host being available, are always transient.
    /// </remarks>
    internal static bool IsTransient(Exception exception) =>
        exception is NpgsqlException and not PostgresException { IsTransient: false };

    /// <summary>Invokes <paramref name="action"/> until it succeeds, waiting exponentially longer between attempts.</summary>
    /// <param name="action">The action to retry, which receives whether this is a retry of a previous attempt.</param>
    /// <param name="shouldRetry">Whether to retry after the exception; if not, the exception is rethrown.</param>
    /// <param name="onRetry">Invoked with the exception and the delay before the next attempt.</param>
    /// <param name="cancellationToken">A token to stop waiting for the next attempt.</param>
    internal static ValueTask<T> RetryAsync<T>(
        Func<bool, ValueTask<T>> action,
        Func<Exception, bool> shouldRetry,
        Action<Exception, TimeSpan> onRetry,
        CancellationToken cancellationToken)
    {
        return RetryAsync(action, DontRetry, Noop, shouldRetry, onRetry, cancellationToken);
    }

    private static void Noop<T>(T t, TimeSpan timeSpan)
    {
    }

    private static bool DontRetry<T>(T _) => false;

    /// <summary>
    /// Invokes <paramref name="action"/> until it succeeds with a result that does not need to be retried,
    /// waiting exponentially longer between attempts.
    /// </summary>
    /// <param name="action">The action to retry, which receives whether this is a retry of a previous attempt.</param>
    /// <param name="shouldRetryResult">Whether to retry after the action returned the result.</param>
    /// <param name="onRetryResult">Invoked with the result and the delay before the next attempt.</param>
    /// <param name="shouldRetry">Whether to retry after the exception; if not, the exception is rethrown.</param>
    /// <param name="onRetry">Invoked with the exception and the delay before the next attempt.</param>
    /// <param name="cancellationToken">A token to stop waiting for the next attempt.</param>
    internal static async ValueTask<TResult> RetryAsync<TResult>(
        Func<bool, ValueTask<TResult>> action,
        Func<TResult, bool> shouldRetryResult,
        Action<TResult, TimeSpan> onRetryResult,
        Func<Exception, bool> shouldRetry,
        Action<Exception, TimeSpan> onRetry,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0;; attempt++)
        {
            var delay = GetExponential(attempt);

            try
            {
                var result = await action(attempt > 0);
                if (!shouldRetryResult(result))
                {
                    return result;
                }

                onRetryResult(result, delay);
            }
            catch (Exception exception) when (shouldRetry(exception))
            {
                onRetry(exception, delay);
            }

            await Task.Delay(delay, cancellationToken);
        }
    }
}

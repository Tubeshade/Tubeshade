using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tubeshade.Server.Services.Background;

public static class SemaphoreSlimExtensions
{
    public static async ValueTask<SemaphoreScope> LockAsync(
        this SemaphoreSlim semaphore,
        CancellationToken cancellationToken = default)
    {
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new SemaphoreScope(semaphore);
    }

    public readonly struct SemaphoreScope(SemaphoreSlim semaphore) : IDisposable
    {
        /// <inheritdoc />
        public void Dispose() => semaphore.Release();
    }
}

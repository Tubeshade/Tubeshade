using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tubeshade.Server.Services;

internal sealed class AsyncDisposableList<T> : List<T>, IAsyncDisposable
    where T : IAsyncDisposable
{
    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var item in this)
        {
            await item.DisposeAsync();
        }
    }
}

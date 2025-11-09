using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Tubeshade.Server.Tests.Integration.Published.Fixtures;

public interface IServerFixture : IAsyncDisposable
{
    string Name { get; }

    Uri BaseAddress { get; }

    HttpClient HttpClient { get; }

    Task InitializeAsync(CancellationToken cancellationToken = default);
}

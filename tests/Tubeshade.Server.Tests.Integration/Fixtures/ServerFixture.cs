using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace Tubeshade.Server.Tests.Integration.Fixtures;

public sealed class ServerFixture : IAsyncDisposable
{
    private readonly PostgreSqlContainer _databaseContainer;
    private readonly List<IContainer> _containers;

    private TubeshadeApplicationFactory? _webApplicationFactory;

    internal string Name { get; }

    public ServerFixture(string version)
    {
        Name = version;

        _databaseContainer = new PostgreSqlBuilder()
            .WithImage($"postgres:{version}")
            .WithTmpfsMount("/var/lib/postgresql/data")
            .WithEnvironment("PGDATA", "/var/lib/postgresql/data")
            .WithCommand("-c", "fsync=off")
            .WithCommand("-c", "synchronous_commit=off")
            .WithCommand("-c", "full_page_writes=off")
            .Build();

        _containers = [_databaseContainer];
    }

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_containers.Select(container => container.StartAsync()));

        var configurationBuilder = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddInMemoryCollection(new List<KeyValuePair<string, string?>>
            {
                new("Database:ConnectionString",
                    $"{_databaseContainer.GetConnectionString()}; Include Error Detail=true"),
            });

        _webApplicationFactory = new TubeshadeApplicationFactory(configurationBuilder.Build());
        _ = _webApplicationFactory.CreateClient();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_webApplicationFactory is not null)
        {
            await _webApplicationFactory.DisposeAsync();
        }

        await Task.WhenAll(_containers.Select(container => container.StopAsync()));
    }

    internal HttpClient CreateHttpClient(params DelegatingHandler[] handlers)
    {
        ArgumentNullException.ThrowIfNull(_webApplicationFactory);
        return _webApplicationFactory.CreateDefaultClient(handlers);
    }
}

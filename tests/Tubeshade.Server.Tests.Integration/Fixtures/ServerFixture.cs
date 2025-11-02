using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.PostgreSql;

namespace Tubeshade.Server.Tests.Integration.Fixtures;

public sealed partial class ServerFixture : IAsyncDisposable
{
    private const string ServerImageName = "tubeshade-integration-tests";

    private readonly IContainer _serverContainer;
    private readonly List<IContainer> _containers;

    internal string Name { get; }

    public ServerFixture(string version)
    {
        Name = version;

        var network = new NetworkBuilder().Build();

        var databaseContainer = new PostgreSqlBuilder()
            .WithImage($"postgres:{version}")
            .WithTmpfsMount("/var/lib/postgresql/data")
            .WithEnvironment("PGDATA", "/var/lib/postgresql/data")
            .WithNetwork(network)
            .WithHostname("database")
            .WithCommand("-c", "fsync=off")
            .WithCommand("-c", "synchronous_commit=off")
            .WithCommand("-c", "full_page_writes=off")
            .Build();

        _serverContainer = new ContainerBuilder()
            .WithImage(ServerImageName)
            .WithNetwork(network)
            .WithEnvironment(
                "Database:ConnectionString",
                "Host=database; Port=5432; Username=postgres; Password=postgres; Include Error Detail=true")
            .WithPortBinding(8080, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged(StartedRegex()))
            .DependsOn(databaseContainer)
            .Build();

        _containers = [databaseContainer, _serverContainer];
    }

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_containers.Select(container => container.StartAsync()));
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(_containers.Select(container => container.StopAsync()));
    }

    internal Uri GetBaseAddress() => new UriBuilder
    {
        Scheme = "http",
        Host = "localhost",
        Port = _serverContainer.GetMappedPublicPort(),
    }.Uri;

    internal HttpClient CreateHttpClient() => new() { BaseAddress = GetBaseAddress() };

    [GeneratedRegex(".*Now listening on.*")]
    private static partial Regex StartedRegex();
}

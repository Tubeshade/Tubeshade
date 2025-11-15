using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.PostgreSql;
using TUnit.Core.Interfaces;

namespace Tubeshade.Server.Tests.Integration.Published;

public sealed partial class WebApplicationFactory : IAsyncInitializer, IAsyncDisposable
{
    private const string ServerImageName = "tubeshade-cover-tests";

    private readonly IContainer _serverContainer;
    private readonly List<IContainer> _containers;

    private bool _serverStopped;

    public WebApplicationFactory()
    {
        var network = new NetworkBuilder().Build();

        var databaseContainer = new PostgreSqlBuilder()
            .WithImage("postgres:18")
            .WithTmpfsMount("/var/lib/postgresql/data")
            .WithEnvironment("PGDATA", "/var/lib/postgresql/data")
            .WithNetwork(network)
            .WithHostname("database")
            .WithPortBinding(5432, true)
            .WithCommand("-c", "fsync=off")
            .WithCommand("-c", "synchronous_commit=off")
            .WithCommand("-c", "full_page_writes=off")
            .Build();

        var reportsDirectory = Path.Combine(CommonDirectoryPath.GetProjectDirectory().DirectoryPath, "TestResults");
        if (!Directory.Exists(reportsDirectory))
        {
            Directory.CreateDirectory(reportsDirectory);
        }

        _serverContainer = new ContainerBuilder()
            .WithImage(ServerImageName)
            .WithNetwork(network)
            .WithBindMount(reportsDirectory, "/reports", AccessMode.ReadWrite)
            .WithEnvironment(
                "Database__ConnectionString",
                "Host=database; Port=5432; Username=postgres; Password=postgres; Include Error Detail=true")
            .WithPortBinding(8080, true)
            .WithWaitStrategy(Wait
                .ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(8080)
                .UntilMessageIsLogged(StartedRegex()))
            .DependsOn(databaseContainer)
            .Build();

        _serverContainer.Stopped += (_, _) => _serverStopped = true;

        _containers = [databaseContainer, _serverContainer];
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await Task.WhenAll(_containers.Select(container => container.StartAsync()));
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.BaseAddress = GetBaseAddress();
            using var response = await httpClient.GetAsync("/api/v1.0/Stop");
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }

        for (var i = 0; i < 30; i++)
        {
            if (_serverStopped)
            {
                break;
            }

            await Task.Delay(1_000);
        }

        await Task.WhenAll(_containers.Select(container => container.StopAsync()));
    }

    internal Uri GetBaseAddress() => new UriBuilder
    {
        Scheme = "http",
        Host = "localhost",
        Port = _serverContainer.GetMappedPublicPort(),
    }.Uri;

    [GeneratedRegex(".*Now listening on.*")]
    private static partial Regex StartedRegex();
}
